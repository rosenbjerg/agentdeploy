const fs = require('fs');
const path = require('path');
const FormData = require('form-data');

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

module.exports = createForm;