import { create } from 'zustand'

interface AppState {
    userId: string
    // Add more state as needed
}

export const useStore = create<AppState>((_set) => ({
    userId: (() => {
        const stored = localStorage.getItem('userId');
        if (stored) {
            console.log('[useStore] Loaded userId from localStorage:', stored);
            return stored;
        }
        const newId = window.crypto.randomUUID();
        localStorage.setItem('userId', newId);
        console.log('[useStore] Generated new userId:', newId);
        return newId;
    })()
}))
