const fs = require('fs');
const path = require('path');
const fetch = require('node-fetch');
const chalk = require('chalk');
const { Command } = require('commander');
const FormData = require('form-data');

const TokenFilePath = './agentd.token';
const program = new Command();

program
    .name('agentd client')
    .version('0.0.1')
    .option('-t, --token <token>', 'Authentication token')
    .option('-v, --variables <keyValuePair...>', 'Add variable')
    .option('-s, --secret-variables <keyValuePair...>', 'Add secret variable')
    .option('-e, --environment-variables <keyValuePair...>', 'Add environment variable')
    .option('-f, --files <keyValuePair...>', 'Add file')
    .command('invoke <command> <serverUrl>')
    .description('Invoke command')
    .action(invokeCommand);

async function invokeCommand(command, serverUrl) {
    const options = program.opts();
    if (!options.token && fs.existsSync(TokenFilePath)) {
        options.token = fs.readFileSync(TokenFilePath, 'utf-8');
    }
    else if (!options.token) {
        console.error(`Token must be provided by placing a file containing the token at the path ${TokenFilePath} or by using the token argument (-t)`);
        process.exit(-1);
    }

    const formdata = createForm(command, options);
    const response = await fetch(`${serverUrl}/rest/invoke`, { method: 'POST', body: formdata, headers: { 'Authorization': `Token ${options.token}` } });

    if (response.status === 404) {
        console.error(`Command ${command} not found`);
        process.exit(-1);
    }
    else if (response.status === 401) {
        console.error(`Token is invalid`);
        process.exit(-1);
    }
    else if (response.status === 400) {
        const json = await response.json();
        console.log(chalk.bold(json.message));
        for (const error of json.errors) {
            console.log(`${error.name} failed: ${error.error}`);
        }
        process.exit(-1);
    }
    else if (response.status === 200) {
        const json = await response.json();

        if (json.command) {
            console.log(`--- ${chalk.bold('Command')} -----------------------------------------------------------`);
            console.log(json.command);
        }

        if (json.output) {
            console.log(`--- ${chalk.bold('Output')} ------------------------------------------------------------`);
            for (const output of json.output) {
                const formatted = `${chalk.dim(output.timestamp.padEnd(29, ' ') + '|')} ${output.output}`;
                if (output.error) console.log(chalk.red(formatted));
                else console.log(formatted);
            }
        }

        if (json.exitCode === 0) console.log(chalk.bold.green('Command executed successfully'));
        else console.log(chalk.bold.red(`Command exited with non-zero exit code: ${json.exitCode}`));
        process.exit(json.exitCode);
    }
    else {
        console.log('Unexpected status', response.status);
        process.exit(-1);
    }
}

const keyValuePairRegex = /^([a-zA-Z0-9-_]+)+=(.*)$/;
function createForm(command, options) {
    const formdata = new FormData();
    formdata.append('command', command);

    let error = false;
    error = addFieldToForm(options.variables || [], formdata, 'variable', 'Variable') || error;
    error = addFieldToForm(options.secretVariables || [], formdata, 'secretVariable', 'Secret variable') || error;
    error = addFieldToForm(options.environmentVariables || [], formdata, 'environment', 'Environment variable') || error;
    error = addFileToForm(options.files || [], formdata) || error;

    if (error) {
        console.error(`Argument validation failed`);
        process.exit(-1);
    }

    return formdata;
}

function addFieldToForm(collection, formdata, kind, formattedKind) {
    let error = false;
    for (const variable of collection){
        if (!keyValuePairRegex.test(variable)){
            console.error(`${formattedKind} is not in correct key-value-pair format ${keyValuePairRegex}`);
            error = true;
        }
        else {
            formdata.append(kind, variable);
        }
    }
    return error;
}

function addFileToForm(collection, formdata) {
    let error = false;
    for (const fileArgument of collection) {
        if (!keyValuePairRegex.test(fileArgument)){
            console.error(`File argument is not in correct key-value-pair format ${keyValuePairRegex}`);
            error = true;
        }
        else {
            const match = fileArgument.match(keyValuePairRegex);
            const key = match[1];
            const filePath = match[2];
            if (!fs.existsSync(filePath)) {
                console.error(`Provided file does not exist: ${filePath}`);
                error = true;
            }
            else {
                formdata.append(key, fs.createReadStream(filePath), { filename: path.basename(filePath) });
            }
        }
    }
    return error;
}

async function main() {
    program.parse(process.argv);
}
main().catch(console.error);