import React from "react";

interface ModalProps {
  isOpen: boolean;
  mergeButtonLoading: boolean;
  onClose: () => void;
  onMerge: () => void;
  onOverwrite: () => void;
}

export default function Modal({ isOpen, mergeButtonLoading, onClose, onMerge, onOverwrite }: ModalProps) {
  if (!isOpen) return null;

  return (
    <div className="overflow-y-auto overflow-x-hidden fixed top-0 right-0 left-0 z-50 flex justify-center items-center w-full md:inset-0 h-[calc(100%-1rem)] max-h-full">
      <div className="relative p-4 w-full max-w-md max-h-full">
        <div className="relative bg-neutral-primary-soft border border-default rounded-base shadow-sm p-4 md:p-6">
          {/* Close Button */}
          <button
            onClick={onClose}
            className="absolute top-3 end-2.5 text-body bg-transparent hover:bg-neutral-tertiary hover:text-heading rounded-base text-sm w-9 h-9 ms-auto inline-flex justify-center items-center"
          >
            <svg
              className="w-5 h-5"
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M6 18L18 6M6 6l12 12"
              />
            </svg>
          </button>

          {/* Content */}
          <div className="p-4 md:p-5 text-center">
            <svg
              className="mx-auto mb-4 text-fg-disabled w-12 h-12"
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M12 9v2m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>

            <h3 className="mb-6 text-body">
              The selected output file already exists. Do you want to merge the changes or overwrite the existing file?
            </h3>

            <div className="flex items-center space-x-4 justify-center">
              <button
                onClick={onMerge}
                className="text-white bg-success box-border border border-transparent hover:bg-success-strong focus:ring-4 focus:ring-success-medium shadow-xs font-medium leading-5 rounded-base text-sm px-4 py-2.5 focus:outline-none"
              >
                {mergeButtonLoading ? 'Merging...' : 'Merge'}
              </button>

                <button
                onClick={onOverwrite}
                className="text-white bg-success box-border border border-transparent hover:bg-success-strong focus:ring-4 focus:ring-success-medium shadow-xs font-medium leading-5 rounded-base text-sm px-4 py-2.5 focus:outline-none"
              >
                Overwrite
              </button>

              <button
                onClick={onClose}
                className="text-body bg-neutral-secondary-medium box-border border border-default-medium hover:bg-neutral-tertiary-medium hover:text-heading focus:ring-4 focus:ring-neutral-tertiary shadow-xs font-medium leading-5 rounded-base text-sm px-4 py-2.5 focus:outline-none"
              >
                No, cancel
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
