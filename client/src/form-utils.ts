import { createReadStream, existsSync } from 'fs';
import { basename } from 'path';
import * as FormData from 'form-data';
import {AgentDeployOptions} from "./agentd-client";

const keyValuePairRegex = /^([a-zA-Z0-9-_]+)+=(.*)$/;

class InputValidationError extends Error {
    public errors: string[];
    constructor(message: string, errors: string[]) {
        super(message);
        this.name = "InputValidationError";
        this.errors = errors;
    }
}

function createForm(scriptName: string, options: AgentDeployOptions) {
    const formdata = new FormData();
    formdata.append('scriptName', scriptName);

    const errors = [];
    addFieldToForm(options.variables || [], formdata, 'variables', 'Variable', errors.push);
    addFieldToForm(options.secretVariables || [], formdata, 'secretVariables', 'Secret variable', errors.push);
    addFieldToForm(options.environmentVariables || [], formdata, 'environmentVariables', 'Environment variable', errors.push);
    addFileToForm(options.files || [], formdata, errors.push);

    if (errors.length > 0)
        throw new InputValidationError('Argument validation failed', errors)

    return formdata;
}

function addFieldToForm(collection: string[], formdata: FormData, formKey: string, formattedKey: string, onError: (error: string)=>void) {
    for (const variable of collection) {
        if (keyValuePairRegex.test(variable)) {
            formdata.append(formKey, variable);
        } else {
            onError(`${formattedKey} is not in correct key-value-pair format ${keyValuePairRegex}`);
        }
    }
}

function addFileToForm(collection: string[], formdata: FormData, onError: (error: string)=>void) {
    for (const fileArgument of collection) {
        if (keyValuePairRegex.test(fileArgument)) {
            const match = fileArgument.match(keyValuePairRegex);
            const key = match[1];
            const filePath = match[2];
            if (filePath && existsSync(filePath)) {
                formdata.append('files', createReadStream(filePath), {filename: `${key}=${basename(filePath)}`});
            } else {
                onError(`Provided file does not exist: ${filePath}`);
            }
        } else {
            onError(`File argument is not in correct key-value-pair format ${keyValuePairRegex}`);
        }
    }
}

export default createForm;