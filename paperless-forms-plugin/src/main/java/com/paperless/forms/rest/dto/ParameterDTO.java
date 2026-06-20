package com.paperless.forms.rest.dto;

public class ParameterDTO {
    private int parameterId;
    private String partCode;
    private String title;
    private String acceptanceCriteria;
    private String controlMethod;
    private int displayOrder;

    public int getParameterId() { return parameterId; }
    public void setParameterId(int parameterId) { this.parameterId = parameterId; }

    public String getPartCode() { return partCode; }
    public void setPartCode(String partCode) { this.partCode = partCode; }

    public String getTitle() { return title; }
    public void setTitle(String title) { this.title = title; }

    public String getAcceptanceCriteria() { return acceptanceCriteria; }
    public void setAcceptanceCriteria(String acceptanceCriteria) { this.acceptanceCriteria = acceptanceCriteria; }

    public String getControlMethod() { return controlMethod; }
    public void setControlMethod(String controlMethod) { this.controlMethod = controlMethod; }

    public int getDisplayOrder() { return displayOrder; }
    public void setDisplayOrder(int displayOrder) { this.displayOrder = displayOrder; }
}
