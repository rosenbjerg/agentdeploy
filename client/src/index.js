import fs from 'fs';

import fetch from 'node-fetch';
import chalk from 'chalk';
import { Command } from 'commander';
import { createForm } from './form-utils.js';
import { v4 as uuidv4 } from 'uuid';
import WebSocket from 'ws';

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

function printFormatted(output) {
    const formatted = `${chalk.dim(output.timestamp.padEnd(29, ' ') + '|')} ${output.output}`;
    if (output.error) console.log(chalk.red(formatted));
    else console.log(formatted);
}

async function handleSuccessResponse(response) {
    const json = await response.json();

    if (json.command) {
        console.log(`--- ${chalk.bold('Command')} -----------------------------------------------------------`);
        console.log(json.command);
    }

    if (json.output && json.output.length) {
        console.log(`--- ${chalk.bold('Output')} ------------------------------------------------------------`);
        for (const output of json.output) {
            printFormatted(output);
        }
    }

    if (json.exitCode === 0) console.log(chalk.bold.green('Command executed successfully'));
    else console.log(chalk.bold.red(`Command exited with non-zero exit code: ${json.exitCode}`));
    process.exit(json.exitCode);
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
    const websocketId = uuidv4();
    if (options.ws) formdata.append('websocket-session-id', websocketId)
    const responsePromise = fetch(`${serverUrl}/rest/invoke`, { method: 'POST', body: formdata, headers: { 'Authorization': `Token ${options.token}` } });
    if (options.ws) {
        setTimeout(() => {
            const wsUrl = `${serverUrl}/websocket/connect/${websocketId}`.replace('https', 'wss').replace('http', 'ws');
            const websocket = new WebSocket(wsUrl);
            websocket.on('open', () => console.log(`--- ${chalk.bold('Output')} ------------------------------------------------------------`));
            websocket.on('message', json => {
                const msg = JSON.parse(json);
                if (msg.event === 'output')
                    printFormatted(msg.data);
            });
        }, 500);
    }

    const response = await responsePromise;
    switch (response.status) {
        case 404: return fail(`Command ${command} not found`);
        case 401: return fail(`Token is invalid`);
        case 400: return await printValidationErrors(response);
        case 200: return await handleSuccessResponse(response);
        default:
            return fail(`Unexpected status: ${response.status}`);
    }
}



async function main() {
    await program.parseAsync(process.argv);
}
main().catch(console.error);