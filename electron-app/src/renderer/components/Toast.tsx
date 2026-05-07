import React, { useEffect } from 'react';

export type ToastType = 'success' | 'error' | 'info' | 'warning';

interface ToastProps {
    message: string;
    type?: ToastType;
    onClose: () => void;
    duration?: number;
}

const toastStyles: Record<ToastType, { bg: string; icon: string }> = {
    success: {
        bg: 'bg-green-500',
        icon: 'check_circle',
    },
    error: {
        bg: 'bg-red-500',
        icon: 'error',
    },
    info: {
        bg: 'bg-blue-500',
        icon: 'info',
    },
    warning: {
        bg: 'bg-yellow-500',
        icon: 'warning',
    },
};

export const Toast: React.FC<ToastProps> = ({
    message,
    type = 'info',
    onClose,
    duration = 3000,
}) => {
    useEffect(() => {
        const timer = setTimeout(onClose, duration);
        return () => clearTimeout(timer);
    }, [duration, onClose]);

    const style = toastStyles[type];

    return (
        <div className="fixed bottom-4 right-4 flex items-center gap-3 px-4 py-3 rounded-lg shadow-lg text-white bg-surface-container-highest animate-pulse">
            <div className={`${style.bg} p-1.5 rounded-full flex items-center justify-center`}>
                <span className="material-symbols-outlined text-white text-base leading-none">
                    {style.icon}
                </span>
            </div>
            <span className="text-sm font-medium">{message}</span>
        </div>
    );
};
