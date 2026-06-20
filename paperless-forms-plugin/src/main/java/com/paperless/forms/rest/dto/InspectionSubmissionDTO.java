package com.paperless.forms.rest.dto;

import java.util.List;

public class InspectionSubmissionDTO {
    private String partCode;
    private String jiraIssueKey;
    private int shift;
    private List<AnswerDTO> answers;

    public String getPartCode() { return partCode; }
    public void setPartCode(String partCode) { this.partCode = partCode; }

    public String getJiraIssueKey() { return jiraIssueKey; }
    public void setJiraIssueKey(String jiraIssueKey) { this.jiraIssueKey = jiraIssueKey; }

    public int getShift() { return shift; }
    public void setShift(int shift) { this.shift = shift; }

    public List<AnswerDTO> getAnswers() { return answers; }
    public void setAnswers(List<AnswerDTO> answers) { this.answers = answers; }
}
