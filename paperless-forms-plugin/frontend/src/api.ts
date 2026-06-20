export interface Part {
  partCode: string;
  partName: string;
  stationCode: string;
  machineCode: string;
  controlPlanNo: string;
}

export interface Parameter {
  parameterId: number;
  partCode: string;
  title: string;
  acceptanceCriteria: string;
  controlMethod: string;
  displayOrder: number;
}

export interface Answer {
  parameterId: number;
  sample1: string;
  sample2: string;
  sample3: string;
  sample4: string;
  sample5: string;
  finalResult: string;
}

export interface InspectionSession {
  partCode: string;
  jiraIssueKey: string;
  shift: number;
  answers: Answer[];
}

export interface Form {
  formCode: string;
  formName: string;
  description: string;
  active: boolean;
}

// Ensure we use the Jira 11 compatible way to get the context path or fallback to empty string for local dev
const getBaseUrl = () => {
  // In Jira 11, we should read from the meta tag instead of legacy AJS global
  const contextPathMeta = document.querySelector('meta[name="ajs-context-path"]');
  if (contextPathMeta) {
    const contextPath = contextPathMeta.getAttribute('content') || '';
    return contextPath + '/rest/paperless/1.0';
  }

  // Fallback for older Jira versions just in case, though meta tag is standard
  // @ts-ignore
  if (typeof AJS !== 'undefined' && AJS.contextPath) {
    // @ts-ignore
    return AJS.contextPath() + '/rest/paperless/1.0';
  }
  return 'http://localhost:8080/rest/paperless/1.0'; // Mock url for local dev if needed
};

export const fetchForms = async (): Promise<Form[]> => {
  const response = await fetch(`${getBaseUrl()}/forms`);
  if (!response.ok) {
    const text = await response.text();
    throw new Error(`Failed to fetch forms: ${text}`);
  }
  return response.json();
};

export const fetchParts = async (): Promise<Part[]> => {
  const response = await fetch(`${getBaseUrl()}/ipi/parts`);
  if (!response.ok) throw new Error('Failed to fetch parts');
  return response.json();
};

export const fetchParameters = async (partCode: string): Promise<Parameter[]> => {
  const response = await fetch(`${getBaseUrl()}/ipi/parameters?partCode=${encodeURIComponent(partCode)}`);
  if (!response.ok) throw new Error('Failed to fetch parameters');
  return response.json();
};

export const submitInspection = async (session: InspectionSession): Promise<void> => {
  const response = await fetch(`${getBaseUrl()}/ipi/sessions`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(session),
  });
  if (!response.ok) throw new Error('Failed to submit inspection');
};
