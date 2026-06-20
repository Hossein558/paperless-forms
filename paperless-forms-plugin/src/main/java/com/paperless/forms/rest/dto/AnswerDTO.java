package com.paperless.forms.rest.dto;

public class AnswerDTO {
    private int parameterId;
    private String sample1;
    private String sample2;
    private String sample3;
    private String sample4;
    private String sample5;
    private String finalResult;

    public int getParameterId() { return parameterId; }
    public void setParameterId(int parameterId) { this.parameterId = parameterId; }

    public String getSample1() { return sample1; }
    public void setSample1(String sample1) { this.sample1 = sample1; }

    public String getSample2() { return sample2; }
    public void setSample2(String sample2) { this.sample2 = sample2; }

    public String getSample3() { return sample3; }
    public void setSample3(String sample3) { this.sample3 = sample3; }

    public String getSample4() { return sample4; }
    public void setSample4(String sample4) { this.sample4 = sample4; }

    public String getSample5() { return sample5; }
    public void setSample5(String sample5) { this.sample5 = sample5; }

    public String getFinalResult() { return finalResult; }
    public void setFinalResult(String finalResult) { this.finalResult = finalResult; }
}
