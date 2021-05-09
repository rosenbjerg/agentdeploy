import { createReadStream, existsSync } from 'fs';
import { basename } from 'path';
import * as FormData from 'form-data';
import {AgentDeployOptions, ErrorCollection, InvocationError} from "./types";

const keyValuePairRegex = /^([a-zA-Z0-9-_]+)=(.*)$/;

function createForm(scriptName: string, options: AgentDeployOptions): FormData {
    const formdata = new FormData();
    formdata.append('scriptName', scriptName);

    const errors: ErrorCollection = {};
    const addError = (name, error) => {
        if (!errors[name]) errors[name] = [];
        errors[name].push(error);
    }

    addFieldToForm(options.variables || [], formdata, 'variables', 'Variable', addError);
    addFieldToForm(options.secretVariables || [], formdata, 'secretVariables', 'Secret variable', addError);
    addFieldToForm(options.environmentVariables || [], formdata, 'environmentVariables', 'Environment variable', addError);
    addFileToForm(options.files || [], formdata, addError);

    if (Object.keys(errors).length > 0)
        throw new InvocationError('Argument validation failed', errors)

    return formdata;
}



function addFieldToForm(collection: string[], formdata: FormData, formKey: string, formattedKey: string, onError: (name, error: string)=>void): void {
    for (const variable of collection) {
        if (keyValuePairRegex.test(variable)) {
            formdata.append(formKey, variable);
        } else {
            onError(formattedKey, `'${variable}' did not pass client-side input validation (${keyValuePairRegex})`);
        }
    }
}

function addFileToForm(collection: string[], formdata: FormData, onError: (name, error: string)=>void): void {
    for (const fileArgument of collection) {
        if (keyValuePairRegex.test(fileArgument)) {
            const match = fileArgument.match(keyValuePairRegex);
            const key = match[1];
            const filePath = match[2];
            if (filePath && existsSync(filePath)) {
                formdata.append('files', createReadStream(filePath), {filename: `${key}=${basename(filePath)}`});
            } else {
                onError('File', `'${key}' does not exist at the path '${filePath}'`);
            }
        } else {
            onError('File', `'${fileArgument}' did not pass client-side input validation (${keyValuePairRegex})`);
        }
    }
}

export default createForm;