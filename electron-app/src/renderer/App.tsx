import { MemoryRouter as Router, Routes, Route } from 'react-router-dom';
import './App.css';
import Main from './pages/Main';
import Merger from './pages/Merger';



export default function App() {
  return (
    <Router>
      <Routes>
        <Route path="/" element={<Main />} />
        <Route path="/merger" element={<Merger />} />
      </Routes>
    </Router>
  );
}