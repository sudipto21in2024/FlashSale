import { create } from 'zustand'

interface AppState {
    userId: string
    // Add more state as needed
}

export const useStore = create<AppState>((_set) => ({
    userId: window.crypto.randomUUID()
}))
