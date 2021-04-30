import {readFileSync, existsSync} from 'fs';
import * as chalk from 'chalk';
import {Command} from 'commander';
import * as dateformat from "dateformat";

import {
    AgentDeployOptions, ErrorCollection,
    ExecutionResult,
    ProcessOutput,
    ProcessOutputHandler,
    ScriptReceivedHandler
} from "./types";
import invokeScript from "./client";

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

const fail = error => {
    console.error(chalk.red(error));
    process.exit(-1);
};

function printErrors(title: string, errors: ErrorCollection): void {
    const errorString = Object.keys(errors).map(prop => `${prop}:\n  ${errors[prop].join('  \n')}`).join('\n');
    const message = `${chalk.bold(title)}\n${errorString}`;
    fail(message);
}

function printFormatted(output: string, time: string, isError: boolean, hideTimestamps: boolean): void {
    const timestamp = hideTimestamps ? '' : chalk.dim(dateformat(new Date(time), "yyyy-mm-dd HH:MM:ss:l o").padEnd(30, ' ') + '| ');
    const formatted = `${timestamp}${output}`;
    if (isError) console.error(chalk.red(formatted));
    else console.log(formatted);
}

async function printExecutionResult(executionResult: ExecutionResult, options: AgentDeployOptions): Promise<void> {
    if (!options.ws && !options.hideScript && executionResult.script) {
        if (!options.hideHeaders) console.log(`--- ${chalk.bold('Script')} ------------------------------------------------------------`);
        console.log(executionResult.script);
    }

    if (!options.ws && executionResult.output && executionResult.output.length) {
        if (!options.hideHeaders) console.log(`--- ${chalk.bold('Output')} ------------------------------------------------------------`);
        for (const output of executionResult.output) {
            printFormatted(output.output, output.timestamp, output.error, options.hideTimestamps);
        }
    }

    if (!options.hideHeaders) {
        if (executionResult.exitCode === 0) console.log(chalk.bold.green('Script executed successfully'));
        else console.log(chalk.bold.red(`Script exited with non-zero exit code: ${executionResult.exitCode}`));
    }
    process.exit(executionResult.exitCode);
}

function prepareWebsocketOutputHandlers(options: AgentDeployOptions): [ProcessOutputHandler, ScriptReceivedHandler] {
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

async function onInvokeCommandCalled(scriptName: string, serverUrl: string) {
    const options = program.opts() as AgentDeployOptions;
    if (!options.token) {
        if (existsSync(TokenFilePath))
            options.token = readFileSync(TokenFilePath, 'utf-8');
        else
            fail(`The token must either be provided by placing a file containing the token at the path ${TokenFilePath} or by using the token argument (-t)`);
    }

    const [onOutputReceived, onScriptReceived] = prepareWebsocketOutputHandlers(options);
    try {
        const executionResult = await invokeScript(scriptName, serverUrl, options, onOutputReceived, onScriptReceived);
        await printExecutionResult(executionResult, options);
    } catch (e) {
        if (e.name === 'InvocationError') {
            printErrors(e.message, e.errors)
        } else {
            fail(`${e.message}`);
        }
    }
}


async function main() {
    await program.parseAsync(process.argv);
}

main().catch(console.error);