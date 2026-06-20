package com.paperless.forms.rest.dto;

public class PartDTO {
    private String partCode;
    private String partName;
    private String stationCode;
    private String machineCode;
    private String controlPlanNo;

    public String getPartCode() { return partCode; }
    public void setPartCode(String partCode) { this.partCode = partCode; }

    public String getPartName() { return partName; }
    public void setPartName(String partName) { this.partName = partName; }

    public String getStationCode() { return stationCode; }
    public void setStationCode(String stationCode) { this.stationCode = stationCode; }

    public String getMachineCode() { return machineCode; }
    public void setMachineCode(String machineCode) { this.machineCode = machineCode; }

    public String getControlPlanNo() { return controlPlanNo; }
    public void setControlPlanNo(String controlPlanNo) { this.controlPlanNo = controlPlanNo; }
}
