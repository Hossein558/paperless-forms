import { useState } from 'react';
import Portal from './Portal';
import InProcessInspection from './InProcessInspection';

const App = () => {
  const [currentForm, setCurrentForm] = useState<string | null>(null);

  if (currentForm === 'IPI') {
    return (
      <div>
        <button 
          className="glass-btn" 
          style={{ margin: '1rem', background: '#555' }} 
          onClick={() => setCurrentForm(null)}
        >
          &larr; Back to Portal
        </button>
        <InProcessInspection />
      </div>
    );
  }

  return <Portal onSelectForm={setCurrentForm} />;
}

export default App;
