import { Outlet, NavLink } from 'react-router-dom';

export default function Layout() {
    return (
        <div className="h-screen flex flex-col  bg-gray-50">
            {/* Header */}
            <header className="bg-white shadow px-6 shrink-0">
                <div className="max-w-6xl mx-auto flex items-center justify-between">
                    <h1 className="text-xl font-bold">My App</h1>

                    <div className="text-sm font-medium text-center text-body">
                        <ul className="flex -mb-px gap-6 pb-2">
                            <li>
                                <NavLink
                                    to="/"
                                    className={({ isActive }) =>
                                        `inline-block pt-4 pb-2 px-4 border-b rounded-t-base ${
                                            isActive
                                                ? 'text-fg-brand border-brand'
                                                : 'border-transparent hover:text-fg-brand hover:border-brand'
                                        }`
                                    }
                                >
                                    Siemens TIA Portal
                                </NavLink>
                            </li>
                            <li>
                                <NavLink
                                    to="/beckhoff"
                                    className={({ isActive }) =>
                                        `inline-block pt-4 pb-2 px-4 border-b rounded-t-base ${
                                            isActive
                                                ? 'text-fg-brand border-brand'
                                                : 'border-transparent hover:text-fg-brand hover:border-brand'
                                        }`
                                    }
                                >
                                    Beckhoff TwinCAT
                                </NavLink>
                            </li>
                        </ul>
                    </div>

                    <div>2</div>
                </div>
            </header>

            {/* Main content (this is where pages render) */}
            <main className="flex-1 flex flex-col overflow-y-auto ">
                <Outlet />
            </main>

            {/* Footer */}
            <footer className="bg-white border-t px-6 py-2 shrink-0">
                <div className="max-w-6xl mx-auto text-sm text-gray-500 flex justify-between">
                    <span>1</span>
                    <span>2</span>
                </div>
            </footer>
        </div>
    );
}
