const fs = require('fs');
const fetch = require('node-fetch');
const chalk = require('chalk');
const WebSocket = require('ws');
const { Command } = require('commander');
const { createForm } = require('./form-utils');
const { v4 } = require('uuid');
const dateFormat = require("dateformat");

const TokenFilePath = './agentd.token';
const program = new Command();

program
    .name('agentd client')
    .version('0.0.2')
    .option('-t, --token <token>', 'Authentication token')
    .option('-v, --variables <keyValuePair...>', 'Add variable')
    .option('-s, --secret-variables <keyValuePair...>', 'Add secret variable')
    .option('-e, --environment-variables <keyValuePair...>', 'Add environment variable')
    .option('-f, --files <keyValuePair...>', 'Add file')
    .option('--ws', 'enable websocket connection for receiving output')
    .option('--hide-timestamps', 'Omit timestamps')
    .option('--hide-headers', 'Omit info headers')
    .option('--hide-command', 'Omit printing command (if available)')
    .command('invoke <command> <serverUrl>')
    .description('Invoke named command on remote server')
    .action(invokeCommand);

const fail = str => {
    console.error(chalk.red(str));
    process.exit(-1);
};

async function printValidationErrors(response) {
    const json = await response.json();
    const message = `${chalk.bold(json.message)}\n${json.errors.map(e => `${e.name} failed: ${e.error}`).join('\n')}`;
    fail(message);
}

function printFormatted(output, time, isError, hideTimestamps) {
    const timestamp = hideTimestamps ? '' : chalk.dim(dateFormat(new Date(time), "yyyy-mm-dd HH:MM:ss:l o").padEnd(30, ' ') + '| ');
    const formatted = `${timestamp}${output}`;
    if (isError) console.log(chalk.red(formatted));
    else console.log(formatted);
}

async function handleSuccessResponse(response, options) {
    const json = await response.json();

    if (!options.ws && !options.hideCommand && json.command) {
        if (!options.hideHeaders)
            console.log(`--- ${chalk.bold('Command')} -----------------------------------------------------------`);
        console.log(json.command);
    }

    if (!options.ws && json.output && json.output.length) {
        if (!options.hideHeaders)
            console.log(`--- ${chalk.bold('Output')} ------------------------------------------------------------`);
        for (const output of json.output) {
            printFormatted(output.output, output.timestamp, output.error, options.hideTimestamps);
        }
    }

    if (!options.hideHeaders) {
        if (json.exitCode === 0) console.log(chalk.bold.green('Command executed successfully'));
        else console.log(chalk.bold.red(`Command exited with non-zero exit code: ${json.exitCode}`));
    }
    process.exit(json.exitCode);
}

function listenForWebsocketCommandOuput(serverUrl, websocketId, hideTimestamps, hideHeaders, hideCommand) {
    const wsUrl = `${serverUrl}/websocket/connect/${websocketId}`.replace('https', 'wss').replace('http', 'ws');
    const websocket = new WebSocket(wsUrl);
    let outputHeaderWritten = false;
    websocket.on('message', json => {
        const msg = JSON.parse(json);
        if (msg.event === 'output'){
            if (!outputHeaderWritten){
                outputHeaderWritten = true;
                if (!hideHeaders) console.log(`--- ${chalk.bold('Output')} ------------------------------------------------------------`);
            }
            printFormatted(msg.data.output, msg.data.timestamp, msg.data.error, hideTimestamps);
        }
        if (msg.event === 'command' && !hideCommand){
            if (!hideHeaders) console.log(`--- ${chalk.bold('Command')} -----------------------------------------------------------`);
            printFormatted(msg.data, '', false, true);
        }
    });
}

async function invokeCommand(command, serverUrl) {
    const options = program.opts();
    if (!options.token) {
        if (fs.existsSync(TokenFilePath))
            options.token = fs.readFileSync(TokenFilePath, 'utf-8');
        else
            fail(`Token must be provided by placing a file containing the token at the path ${TokenFilePath} or by using the token argument (-t)`);
    }

    const formdata = createForm(command, options);
    const websocketId = v4();
    if (options.ws) formdata.append('websocket-session-id', websocketId)
    const responsePromise = fetch(`${serverUrl}/rest/invoke`, { method: 'POST', body: formdata, headers: { 'Authorization': `Token ${options.token}` } });
    if (options.ws) listenForWebsocketCommandOuput(serverUrl, websocketId, options.hideTimestamps, options.hideHeaders, options.hideCommand);

    const response = await responsePromise;
    switch (response.status) {
        case 404: return fail(`Command ${command} not found`);
        case 401: return fail(`Token is invalid`);
        case 400: return await printValidationErrors(response);
        case 200: return await handleSuccessResponse(response, options);
        default:
            return fail(`Unexpected status: ${response.status}`);
    }
}



async function main() {
    await program.parseAsync(process.argv);
}
main().catch(console.error);