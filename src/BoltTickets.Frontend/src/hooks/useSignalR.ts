import { HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr"
import { useEffect, useState } from "react"

export function useSignalR(hubUrl: string) {
    const [connection, setConnection] = useState<HubConnection | null>(null)

    useEffect(() => {
        console.log(`[SignalR] Creating connection to ${hubUrl}`)
        const connect = new HubConnectionBuilder()
            .withUrl(hubUrl)
            .configureLogging(LogLevel.Trace)
            .withAutomaticReconnect()
            .build()

        setConnection(connect)
    }, [hubUrl])

    useEffect(() => {
        if (connection) {
            connection
                .start()
                .then(() => console.log(`SignalR Connected to ${hubUrl}`))
                .catch((err) => console.error("SignalR Connection Error: ", err))

            return () => {
                connection.stop();
            }
        }
    }, [connection, hubUrl])

    return connection
}
