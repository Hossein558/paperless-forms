import { useState, useEffect } from 'react';
import { fetchForms } from './api';
import type { Form } from './api';

interface PortalProps {
  onSelectForm: (formCode: string) => void;
}

const Portal = ({ onSelectForm }: PortalProps) => {
  const [forms, setForms] = useState<Form[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    setLoading(true);
    fetchForms()
      .then(setForms)
      .catch(err => {
        console.error(err);
        setError('Failed to load forms.');
      })
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="glass-container">
      <h1 style={{ marginBottom: '1.5rem', color: 'var(--primary-color)' }}>
        Paperless Forms Portal
      </h1>
      
      {loading && <p>Loading forms...</p>}
      {error && <p style={{ color: 'red' }}>{error}</p>}

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))', gap: '1.5rem' }}>
        {forms.map(form => (
          <div 
            key={form.formCode} 
            className="glass-panel" 
            style={{ cursor: 'pointer', display: 'flex', flexDirection: 'column', height: '100%' }}
            onClick={() => onSelectForm(form.formCode)}
          >
            <h3 style={{ marginTop: 0, color: 'var(--primary-color)' }}>{form.formName}</h3>
            <p style={{ color: 'var(--text-light)', flexGrow: 1 }}>{form.description}</p>
            <div style={{ marginTop: '1rem', textAlign: 'right' }}>
              <button className="glass-btn" style={{ padding: '0.5rem 1rem', fontSize: '0.9rem' }}>
                Open Form
              </button>
            </div>
          </div>
        ))}
        {!loading && forms.length === 0 && !error && (
          <p>No active forms available.</p>
        )}
      </div>
    </div>
  );
};

export default Portal;
