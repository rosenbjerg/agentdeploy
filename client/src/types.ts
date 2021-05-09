
export type ProcessOutputHandler = (event: ProcessOutput) => void;
export type ScriptReceivedHandler = (event: string) => void;
export type ErrorCollection = { [name: string] : string[] }

export interface AgentDeployOptions {
    token: string
    variables: string[]
    secretVariables: string[]
    environmentVariables: string[]
    files: string[]
    ws: boolean
    interactive: boolean
    hideTimestamps: boolean
    hideHeaders: boolean
    hideScript: boolean
}

export interface ProcessOutput {
    timestamp: string
    output: string
    error: boolean
}

export interface ExecutionResult {
    output: ProcessOutput[]
    script: string
    exitCode: number
}

export interface FailedInvocation {
    title: string
    errors: ErrorCollection
}

export class InvocationError extends Error {
    public errors: ErrorCollection;
    constructor(message: string, errors: ErrorCollection) {
        super(message);
        this.name = "InvocationError";
        this.errors = errors;
    }
}