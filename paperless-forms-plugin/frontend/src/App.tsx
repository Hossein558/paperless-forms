import { useState } from 'react';
import Portal from './Portal';
import InProcessInspection from './InProcessInspection';

const App = () => {
  const [currentForm, setCurrentForm] = useState<string | null>(() => {
    console.log("Current path:", window.location.pathname);
    // If the old URL is used, default to the IPI form
    if (window.location.pathname.includes('/inspection')) {
      console.log("Routing to /inspection (IPI form)");
      return 'IPI';
    }
    console.log("Routing to portal");
    return null;
  });

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
