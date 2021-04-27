import * as WebSocket from "ws";
import createForm from "./form-utils";
import { v4 } from "uuid";
import fetch from "node-fetch";

export interface AgentDeployOptions {
    token: string
    variables: string[]
    secretVariables: string[]
    environmentVariables: string[]
    files: string[]
    ws: boolean
    hideTimestamps: boolean
    hideHeaders: boolean
    hideScript: boolean
}

export interface ProcessOutput {
    timestamp: string
    output: string
    error: boolean
}

export type ProcessOutputHandler = (event: ProcessOutput) => void;
export type ScriptReceivedHandler = (event: string) => void;

export function subscribeToWebsocketEvents(wsUrl: string, onOutput: ProcessOutputHandler, onScript: ScriptReceivedHandler) {
    const websocket = new WebSocket(wsUrl);
    websocket.onmessage = json => {
        const msg = JSON.parse(json.data.toString());
        switch (msg.event) {
            case 'output': return onOutput(msg.data);
            case 'script': return onScript(msg.data);
        }
    };
}

export function invokeScript(scriptName: string, serverUrl: string, options: AgentDeployOptions, onOutput: ProcessOutputHandler, onScript: ScriptReceivedHandler) {
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
    return responsePromise;
}
