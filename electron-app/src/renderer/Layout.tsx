import { Outlet, NavLink } from 'react-router-dom';

export default function Layout() {
    return (
        <div className="h-screen flex flex-col bg-surface text-surface font-body ">
            {/* Header */}
            <header className="h-14 px-6 shrink-0 bg-surface-container-lowest/80 backdrop-blur-xl shadow-2xl shadow-black/50">
                <div className=" flex items-center justify-between">
                    <h1 className="w-32 text-center text-xl font-bold text-primary tracking-tighter uppercase ">
                        Dräger App
                    </h1>

                    <div className="text-sm font-medium text-center text-body">
                        <ul className="flex -mb-px gap-6 pb-2">
                            <li className="mt-1">
                                <NavLink
                                    to="/"
                                    className={({ isActive }) =>
                                        `inline-block pt-4 pb-2 px-4 border-b rounded-t-base ${
                                            isActive
                                                ? 'text-primary border-b-2 border-primary-400'
                                                : 'border-transparent text-textcolor/60 hover:text-textcolor transition-colors'
                                        }`
                                    }
                                >
                                    Siemens TIA Portal
                                </NavLink>
                            </li>
                            <li className="mt-1">
                                <NavLink
                                    to="/beckhoff"
                                    className={({ isActive }) =>
                                        `inline-block pt-4 pb-2 px-4 border-b rounded-t-base ${
                                            isActive
                                                ? 'text-primary border-b-2 border-primary-400'
                                                : 'border-transparent text-textcolor/60 hover:text-textcolor transition-colors'
                                        }`
                                    }
                                >
                                    Beckhoff TwinCAT
                                </NavLink>
                            </li>
                        </ul>
                    </div>

                    <div className="w-32 text-textcolor text-center">2</div>
                </div>
            </header>

            {/* Main content (this is where pages render) */}
            <main className="flex-1 flex flex-col  items-center overflow-y-auto scrollbar-custom w-full min-h-0 ">
                <Outlet />
            </main>
        </div>
    );
}
