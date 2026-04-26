import { MemoryRouter as Router, Routes, Route } from 'react-router-dom';
import './App.css';
import Siemens from './pages/Siemens';
import Beckhoff from './pages/Beckhoff';
import 'flowbite';
import Layout from './Layout';

export default function App() {
    return (
        <Router>
            <Routes>
                <Route element={<Layout />}>
                    <Route path="/" element={<Siemens />} />
                    <Route path="/beckhoff" element={<Beckhoff />} />
                </Route>
            </Routes>
        </Router>
    );
}
