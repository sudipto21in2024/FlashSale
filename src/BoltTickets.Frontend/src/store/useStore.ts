import { create } from 'zustand'

interface AppState {
    userId: string
    // Add more state as needed
}

export const useStore = create<AppState>((_set) => ({
    userId: (() => {
        const stored = localStorage.getItem('userId');
        if (stored) return stored;
        const newId = window.crypto.randomUUID();
        localStorage.setItem('userId', newId);
        return newId;
    })()
}))
