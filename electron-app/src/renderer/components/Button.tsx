import React from 'react';

type ButtonVariant = 'default' | 'primary' | 'danger';

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
    variant?: ButtonVariant;
}

const baseStyles =
    'border px-6 py-2.5 text-xs font-bold uppercase tracking-widest transition-colors active:scale-95';

const variantStyles: Record<ButtonVariant, string> = {
    default: 'border-outline text-surface-inverse hover:bg-surface-bright',
    primary: 'border-blue-500 text-blue-500 hover:bg-blue-500 hover:text-white',
    danger: 'border-red-500 text-red-500 hover:bg-red-500 hover:text-white',
};

export const Button: React.FC<ButtonProps> = ({
    variant = 'default',
    className = '',
    disabled,
    ...props
}) => {
    return (
        <button
            className={`
                ${baseStyles}
                ${variantStyles[variant]}
                ${!disabled ? 'hover:cursor-pointer' : 'cursor-not-allowed opacity-50 hover:bg-transparent'}
                ${className}
            `}
            disabled={disabled}
            {...props}
        />
    );
};
