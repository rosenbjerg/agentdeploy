import * as WebSocket from "ws";
import createForm from "./form-utils";
import { v4 } from "uuid";
import fetch from "node-fetch";
import {
    AgentDeployOptions,
    ExecutionResult, FailedInvocation, InvocationError,
    ProcessOutputHandler,
    ScriptReceivedHandler
} from "./types";

function subscribeToWebsocketEvents(wsUrl: string, onOutput: ProcessOutputHandler, onScript: ScriptReceivedHandler): void {
    const websocket = new WebSocket(wsUrl);
    websocket.onmessage = json => {
        const msg = JSON.parse(json.data.toString());
        switch (msg.event) {
            case 'output': return onOutput(msg.data);
            case 'script': return onScript(msg.data);
        }
    };
}

export default async function invokeScript(scriptName: string, serverUrl: string, options: AgentDeployOptions, onOutput: ProcessOutputHandler, onScript: ScriptReceivedHandler): Promise<ExecutionResult> {
    const formdata = createForm(scriptName, options);
    const websocketId = v4();
    if (options.ws) formdata.append('websocket-session-id', websocketId)
    const responsePromise = fetch(`${serverUrl}/rest/invoke`, {
        method: 'POST',
        body: formdata,
        headers: {'Authorization': `Token ${options.token}`}
    });
    if (options.ws) {
        const wsUrl = `${serverUrl}/websocket/connect/${websocketId}`.replace('https', 'wss').replace('http', 'ws');
        subscribeToWebsocketEvents(wsUrl, onOutput, onScript);
    }

    const response = await responsePromise;
    if (response.status === 200) {
        return await response.json() as ExecutionResult;
    }
    else {
        switch (response.status) {
            case 400:
                const failedInvocation = await response.json() as FailedInvocation;
                throw new InvocationError(failedInvocation.title, failedInvocation.errors);
            case 401: throw new Error(`The provided token is invalid`);
            case 404: throw new Error(`No script named '${scriptName}' is available`);
            case 423: throw new Error(await response.text());
            default: throw new Error(`Unexpected response status code: ${response.status}`);
        }
    }
}
