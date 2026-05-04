import React from 'react';

type ButtonVariant = 'default' | 'primary' | 'danger';

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
    variant?: ButtonVariant;
}

const baseStyles =
    ' px-6 py-2.5 text-xs font-bold uppercase tracking-widest transition-colors active:scale-95 transition-all';

const variantStyles: Record<ButtonVariant, string> = {
    default:
        'border border-outline text-textcolor hover:bg-surface-container-highest',
    primary:
        'bg-primary text-surface px-10 text-xs font-black shadow-lg shadow-primary/20 ',
    danger: 'border border-error text-error hover:bg-error hover:text-white',
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
                ${!disabled ? 'hover:cursor-pointer hover:brightness-110' : 'cursor-not-allowed opacity-20 active:scale-100'}
                ${className}
            `}
            disabled={disabled}
            {...props}
            type="button"
        />
    );
};
