import { useEffect, useState } from 'react'
import { useSignalR } from '../hooks/useSignalR'
import { useStore } from '../store/useStore'
import { Ticket, ShoppingCart, Loader2 } from 'lucide-react'

// Demo Ticket ID
const TICKET_ID = "d2bd2dba-4d23-456a-931d-531e2d7e6822"
const API_URL = "http://localhost:5162"

export function TicketDashboard() {
    const connection = useSignalR(`${API_URL}/hubs/tickets`)
    const { userId } = useStore()
    const [available, setAvailable] = useState(5000)
    const [loading, setLoading] = useState(false)
    const [status, setStatus] = useState("")

    useEffect(() => {
        const fetchInitialCount = async () => {
            try {
                const res = await fetch(`${API_URL}/api/v1/tickets/inventory/${TICKET_ID}`)
                if (res.ok) {
                    const data = await res.json()
                    setAvailable(data.AvailableCount || data.availableCount)
                }
            } catch (e) {
                console.error("Error fetching initial count:", e)
            }
        }
        fetchInitialCount()
    }, [])

    useEffect(() => {
        console.log(`[TicketDashboard] Setting up listeners for connection, userId: ${userId}`)
        if (connection) {
            console.log(`[TicketDashboard] Connection state: ${connection.state}`)
            const joinGroup = () => {
                console.log(`[TicketDashboard] Joining group for userId: ${userId}`)
                connection.invoke("JoinTicketGroup", userId)
                    .then(() => console.log("Joined notification group:", userId))
                    .catch(err => console.error("Error joining group", err));
            };

            if (connection.state === "Connected") {
                joinGroup();
            } else {
                connection.onreconnected(joinGroup);
                // Also wait for initial connect
                const startInterval = setInterval(() => {
                    if (connection.state === "Connected") {
                        console.log(`[TicketDashboard] Connection now connected, joining group`)
                        joinGroup();
                        clearInterval(startInterval);
                    }
                }, 500);
                return () => clearInterval(startInterval);
            }

            console.log(`[TicketDashboard] Setting up event listeners`)
            connection.on("inventoryupdated", (data: any) => {
                console.log("[TicketDashboard] Received inventoryupdated:", data);
                let count = 0;
                if (typeof data === 'number') {
                    count = data;
                } else {
                    count = data.AvailableCount ?? data.availableCount ?? 0;
                }
                console.log("Updated count:", count);
                setAvailable(count)
            })

            connection.on("bookingconfirmed", (data: any) => {
                console.log("[TicketDashboard] Received bookingconfirmed:", data);
                const bId = data.BookingId || data.bookingId;
                setStatus(`Confirmed! Booking ID: ${bId.slice(0, 8)}...`)
            })

            connection.on("anybookingconfirmed", (data: any) => {
                console.log("[TicketDashboard] Received anybookingconfirmed:", data);
                const uId = data.UserId || data.userId;
                console.log(`[TicketDashboard] Checking userId match: received ${uId}, local ${userId}`)
                if (uId === userId) {
                    console.log("[TicketDashboard] UserId matches, updating status")
                    const bId = data.BookingId || data.bookingId;
                    setStatus(`Confirmed! Booking ID: ${bId.slice(0, 8)}...`);
                } else {
                    console.log("[TicketDashboard] UserId does not match, ignoring")
                }
            })

            connection.on("heartbeat", (data: any) => {
                console.log("[TicketDashboard] Received heartbeat:", data);
            })
        }
        return () => {
            console.log(`[TicketDashboard] Cleaning up listeners`)
            connection?.off("inventoryupdated")
            connection?.off("bookingconfirmed")
            connection?.off("anybookingconfirmed")
            connection?.off("heartbeat")
        }
    }, [connection, userId]);

    // Debug connection state changes
    useEffect(() => {
        if (!connection) return;
        const logState = () => console.log('SignalR connection state:', connection.state);
        logState();
        connection.onclose(() => console.log('SignalR connection closed'));
        connection.onreconnecting(() => console.log('SignalR reconnecting'));
        connection.onreconnected(() => console.log('SignalR reconnected'));
        return () => {
            connection.off('close');
            connection.off('reconnecting');
            connection.off('reconnected');
        };
    }, [connection]);

    const handleBuy = async () => {
        setLoading(true)
        setStatus("Processing...")
        try {
            const res = await fetch(`${API_URL}/api/v1/tickets/book`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ ticketId: TICKET_ID, userId })
            })

            if (res.ok) {
                setStatus("Booking Intent Queued! Check your email.")
            } else {
                const data = await res.json()
                setStatus(`Failed: ${data.detail || "Unknown error"}`)
            }
        } catch (e) {
            console.error(e)
            setStatus("Network Error.")
        }
        setLoading(false)
    }

    return (
        <div className="min-h-screen bg-slate-900 text-white flex items-center justify-center p-4">
            <div className="max-w-md w-full bg-slate-800 rounded-xl shadow-2xl overflow-hidden border border-slate-700">
                <div className="relative h-48 bg-purple-600 flex items-center justify-center">
                    <Ticket className="w-24 h-24 text-white/20 absolute" />
                    <h1 className="text-4xl font-bold relative z-10 text-white drop-shadow-md">BoltTickets</h1>
                </div>

                <div className="p-6 space-y-6">
                    <div className="text-center space-y-2">
                        <h2 className="text-2xl font-semibold text-purple-400">Flash Sale Event</h2>
                        <p className="text-slate-400">Exclusive Concert Tickets</p>
                    </div>

                    <div className="bg-slate-900 rounded-lg p-4 flex flex-col items-center">
                        <span className="text-slate-400 text-sm uppercase tracking-wider">Tickets Remaining</span>
                        <span className={`text-5xl font-mono font-bold my-2 ${available === 0 ? 'text-red-500' : 'text-green-400'}`}>
                            {available.toLocaleString()}
                        </span>
                    </div>

                    <div className="space-y-4">
                        <button
                            onClick={handleBuy}
                            disabled={loading || available === 0}
                            className="w-full bg-purple-600 hover:bg-purple-700 disabled:bg-slate-600 disabled:cursor-not-allowed text-white font-bold py-4 rounded-lg transition-all transform active:scale-95 flex items-center justify-center gap-2"
                        >
                            {loading ? <Loader2 className="animate-spin" /> : <ShoppingCart />}
                            {available === 0 ? "SOLD OUT" : "BUY NOW"}
                        </button>

                        {status && (
                            <div className="text-center text-sm font-medium animate-pulse text-yellow-400">
                                {status}
                            </div>
                        )}
                    </div>

                    <div className="text-xs text-center text-slate-600">
                        Session ID: <span className="font-mono">{userId.slice(0, 8)}...</span>
                    </div>
                </div>
            </div>
        </div>
    )
}
