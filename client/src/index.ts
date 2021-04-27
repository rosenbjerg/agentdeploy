import {readFileSync, existsSync} from 'fs';
import * as chalk from 'chalk';
import {Command} from 'commander';
import * as dateFormat from "dateFormat";

import {
    AgentDeployOptions,
    invokeScript,
    ProcessOutput,
    ProcessOutputHandler,
    ScriptReceivedHandler
} from "./agentd-client";

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
    .description('Invoke named script on remote server')
    .action(onInvokeCommandCalled);

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

function prepareWebsocketOutputHandler(options: AgentDeployOptions): [ProcessOutputHandler, ScriptReceivedHandler] {
    let outputHeaderWritten = false;
    const onOutput = (event: ProcessOutput) => {
        if (!outputHeaderWritten && !options.hideHeaders) {
            outputHeaderWritten = true;
            console.log(`--- ${chalk.bold('Output')} ------------------------------------------------------------`);
        }
        printFormatted(event.output, event.timestamp, event.error, options.hideTimestamps);
    };

    const onScript = (event: string) => {
        if (options.hideScript) return;
        if (!options.hideHeaders) console.log(`--- ${chalk.bold('Script')} ------------------------------------------------------------`);
        printFormatted(event, '', false, true);
    }

    return [onOutput, onScript];
}

async function onInvokeCommandCalled(scriptName, serverUrl) {
    const options = <AgentDeployOptions> program.opts();
    if (!options.token) {
        if (existsSync(TokenFilePath))
            options.token = readFileSync(TokenFilePath, 'utf-8');
        else
            fail(`token must be provided by placing a file containing the token at the path ${TokenFilePath} or by using the token argument (-t)`);
    }

    const [onOutputReceived, onScriptReceived] = prepareWebsocketOutputHandler(options);
    try {
        const response = await invokeScript(scriptName, serverUrl, options, onOutputReceived, onScriptReceived);
        switch (response.status) {
            case 400: return await printValidationErrors(response);
            case 401: return fail(`token is invalid`);
            case 404: return fail(`script '${scriptName}' not found`);
            case 423: return fail(await response.text());
            case 200: return await handleSuccessResponse(response, options);
            default: return fail(response.status);
        }
    } catch (e) {
        if (e.name === 'InputValidationError') {
            console.error(chalk.red(`error: ${e.message}`));
            for (const validationError of e.errors) {
                console.error(validationError);
            }
            process.exit(-1);
        } else {
            fail(`${e.message}`);
        }
    }
}


async function main() {
    await program.parseAsync(process.argv);
}

main().catch(console.error);