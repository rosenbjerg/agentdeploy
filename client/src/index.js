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
    .option('--hide-script', 'Omit printing script (if available)')
    .command('invoke <scriptName> <serverUrl>')
    .description('Invoke named command on remote server')
    .action(invokeScript);

const fail = str => {
    console.error(chalk.red('error: ' + str));
    process.exit(-1);
};

async function printValidationErrors(response) {
    const text = await response.text();
    const json = JSON.parse(text);
    if (!json.errors.length) console.error(text);
    const message = `${chalk.bold(json.message)}\n${json.errors.map(e => `${e.name} failed: ${e.error}`).join('\n')}`;
    fail(message);
}

function printFormatted(output, time, isError, hideTimestamps) {
    const timestamp = hideTimestamps ? '' : chalk.dim(dateFormat(new Date(time), "yyyy-mm-dd HH:MM:ss:l o").padEnd(30, ' ') + '| ');
    const formatted = `${timestamp}${output}`;
    if (isError) console.error(chalk.red(formatted));
    else console.log(formatted);
}

async function handleSuccessResponse(response, options) {
    const json = await response.json();

    if (!options.ws && !options.hideScript && json.script) {
        if (!options.hideHeaders) console.log(`--- ${chalk.bold('Script')} -----------------------------------------------------------`);
        console.log(json.script);
    }

    if (!options.ws && json.output && json.output.length) {
        if (!options.hideHeaders) console.log(`--- ${chalk.bold('Output')} ------------------------------------------------------------`);
        for (const output of json.output) {
            printFormatted(output.output, output.timestamp, output.error, options.hideTimestamps);
        }
    }

    if (!options.hideHeaders) {
        if (json.exitCode === 0) console.log(chalk.bold.green('Script executed successfully'));
        else console.log(chalk.bold.red(`Script exited with non-zero exit code: ${json.exitCode}`));
    }
    process.exit(json.exitCode);
}

function listenForWebsocketCommandOuput(serverUrl, websocketId, hideTimestamps, hideHeaders, hideScript) {
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
        if (msg.event === 'script' && !hideScript){
            if (!hideHeaders) console.log(`--- ${chalk.bold('Script')} -----------------------------------------------------------`);
            printFormatted(msg.data, '', false, true);
        }
    });
}

function invoke(scriptName, options, serverUrl) {
    const formdata = createForm(scriptName, options);
    const websocketId = v4();
    if (options.ws) formdata.append('websocket-session-id', websocketId)
    const responsePromise = fetch(`${serverUrl}/rest/invoke`, {
        method: 'POST',
        body: formdata,
        headers: {'Authorization': `Token ${options.token}`}
    });
    if (options.ws) listenForWebsocketCommandOuput(serverUrl, websocketId, options.hideTimestamps, options.hideHeaders, options.hideCommand);
    return responsePromise;
}

async function invokeScript(scriptName, serverUrl) {
    const options = program.opts();
    if (!options.token) {
        if (fs.existsSync(TokenFilePath))
            options.token = fs.readFileSync(TokenFilePath, 'utf-8');
        else
            fail(`token must be provided by placing a file containing the token at the path ${TokenFilePath} or by using the token argument (-t)`);
    }
    const responsePromise = invoke(scriptName, options, serverUrl);

    try {
        const response = await responsePromise;
        switch (response.status) {
            case 400: return await printValidationErrors(response);
            case 401: return fail(`token is invalid`);
            case 404: return fail(`script '${scriptName}' not found`);
            case 423: return fail(await response.text());
            case 200: return await handleSuccessResponse(response, options);
            default:
                return fail(response.status);
        }
    } catch (e) {
        return fail(`${e.message}`);
    }
}



async function main() {
    await program.parseAsync(process.argv);
}
main().catch(console.error);