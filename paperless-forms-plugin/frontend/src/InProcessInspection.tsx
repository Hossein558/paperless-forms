import { useEffect, useState } from 'react';
import './index.css';
import { fetchParts, fetchParameters, submitInspection } from './api';
import type { Part, Parameter, Answer } from './api';

function InProcessInspection() {
  const [parts, setParts] = useState<Part[]>([]);
  const [selectedPart, setSelectedPart] = useState<string>('');
  const [parameters, setParameters] = useState<Parameter[]>([]);
  const [answers, setAnswers] = useState<Record<number, Answer>>({});
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState('');

  // We can get Jira issue key if this app is embedded in a Jira issue panel
  // For now, we simulate getting it from URL or window object
  const getIssueKey = () => {
    const match = window.location.href.match(/([A-Z]+-[0-9]+)/);
    return match ? match[1] : 'UNKNOWN-1';
  };

  useEffect(() => {
    fetchParts()
      .then(setParts)
      .catch(err => console.error(err));
  }, []);

  useEffect(() => {
    if (selectedPart) {
      setLoading(true);
      fetchParameters(selectedPart)
        .then(params => {
          setParameters(params);
          const initialAnswers: Record<number, Answer> = {};
          params.forEach(p => {
            initialAnswers[p.parameterId] = {
              parameterId: p.parameterId,
              sample1: '', sample2: '', sample3: '', sample4: '', sample5: '',
              finalResult: 'OK'
            };
          });
          setAnswers(initialAnswers);
        })
        .catch(err => console.error(err))
        .finally(() => setLoading(false));
    } else {
      setParameters([]);
    }
  }, [selectedPart]);

  const handleAnswerChange = (paramId: number, field: keyof Answer, value: string) => {
    setAnswers(prev => ({
      ...prev,
      [paramId]: {
        ...prev[paramId],
        [field]: value
      }
    }));
  };

  const handleSubmit = async () => {
    setLoading(true);
    setMessage('');
    try {
      await submitInspection({
        partCode: selectedPart,
        jiraIssueKey: getIssueKey(),
        shift: 1, // default shift
        answers: Object.values(answers)
      });
      setMessage('Inspection saved successfully!');
      // Reset form
      setSelectedPart('');
    } catch (err) {
      setMessage('Failed to save inspection.');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="glass-container">
      <h1 style={{ marginBottom: '1.5rem', color: 'var(--primary-color)' }}>
        In-Process Inspection
      </h1>

      <div className="glass-panel">
        <label style={{ fontWeight: 600, display: 'block', marginBottom: '0.5rem' }}>Select Part:</label>
        <select 
          className="glass-input" 
          value={selectedPart} 
          onChange={(e) => setSelectedPart(e.target.value)}
        >
          <option value="">-- Choose a part --</option>
          {parts.map(p => (
            <option key={p.partCode} value={p.partCode}>
              {p.partName} ({p.partCode}) - {p.machineCode}
            </option>
          ))}
        </select>
      </div>

      {loading && <p>Loading...</p>}

      {parameters.length > 0 && !loading && (
        <div className="glass-panel" style={{ overflowX: 'auto' }}>
          <table>
            <thead>
              <tr>
                <th>Parameter</th>
                <th>Acceptance Criteria</th>
                <th>Sample 1</th>
                <th>Sample 2</th>
                <th>Sample 3</th>
                <th>Sample 4</th>
                <th>Sample 5</th>
                <th>Result</th>
              </tr>
            </thead>
            <tbody>
              {parameters.map(p => (
                <tr key={p.parameterId}>
                  <td style={{ fontWeight: 500 }}>{p.title}</td>
                  <td style={{ fontSize: '0.9rem', color: 'var(--text-light)' }}>{p.acceptanceCriteria}</td>
                  {[1, 2, 3, 4, 5].map(i => (
                    <td key={i}>
                      <input 
                        type="text" 
                        className="glass-input" 
                        style={{ width: '60px', padding: '0.5rem' }}
                        value={answers[p.parameterId][`sample${i}` as keyof Answer] as string}
                        onChange={(e) => handleAnswerChange(p.parameterId, `sample${i}` as keyof Answer, e.target.value)}
                      />
                    </td>
                  ))}
                  <td>
                    <select 
                      className="glass-input" 
                      style={{ padding: '0.5rem' }}
                      value={answers[p.parameterId].finalResult}
                      onChange={(e) => handleAnswerChange(p.parameterId, 'finalResult', e.target.value)}
                    >
                      <option value="OK">OK</option>
                      <option value="NOK">NOK</option>
                    </select>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          <div style={{ marginTop: '2rem', textAlign: 'right' }}>
            <button className="glass-btn" onClick={handleSubmit} disabled={loading}>
              Submit Inspection
            </button>
          </div>
        </div>
      )}

      {message && (
        <div className="glass-panel" style={{ 
          background: message.includes('success') ? 'rgba(76, 175, 80, 0.2)' : 'rgba(244, 67, 54, 0.2)'
        }}>
          <strong>{message}</strong>
        </div>
      )}
    </div>
  );
}

export default InProcessInspection;
