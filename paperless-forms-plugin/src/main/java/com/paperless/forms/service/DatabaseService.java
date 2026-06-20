package com.paperless.forms.service;

import com.paperless.forms.rest.dto.AnswerDTO;
import com.paperless.forms.rest.dto.FormDTO;
import com.paperless.forms.rest.dto.InspectionSubmissionDTO;
import com.paperless.forms.rest.dto.ParameterDTO;
import com.paperless.forms.rest.dto.PartDTO;

import javax.naming.InitialContext;
import javax.sql.DataSource;
import java.sql.*;
import java.util.ArrayList;
import java.util.List;

import javax.inject.Named;

@Named
public class DatabaseService {

    private Connection getConnection() throws Exception {
        InitialContext initialContext = new InitialContext();
        DataSource dataSource = (DataSource) initialContext.lookup("java:comp/env/jdbc/JiraDS");
        return dataSource.getConnection();
    }

    public List<FormDTO> getForms() throws Exception {
        List<FormDTO> forms = new ArrayList<>();
        String sql = "SELECT FormCode, FormName, Description, IsActive FROM PM_Forms WHERE IsActive = 1";
        try (Connection conn = getConnection();
             PreparedStatement stmt = conn.prepareStatement(sql);
             ResultSet rs = stmt.executeQuery()) {
            while (rs.next()) {
                FormDTO form = new FormDTO();
                form.setFormCode(rs.getString("FormCode"));
                form.setFormName(rs.getString("FormName"));
                form.setDescription(rs.getString("Description"));
                form.setActive(rs.getBoolean("IsActive"));
                forms.add(form);
            }
        }
        return forms;
    }

    public List<PartDTO> getParts() throws Exception {
        List<PartDTO> parts = new ArrayList<>();
        String sql = "SELECT PartCode, PartName, StationCode, MachineCode, ControlPlanNo FROM PM_Parts";
        try (Connection conn = getConnection();
             PreparedStatement stmt = conn.prepareStatement(sql);
             ResultSet rs = stmt.executeQuery()) {
            while (rs.next()) {
                PartDTO part = new PartDTO();
                part.setPartCode(rs.getString("PartCode"));
                part.setPartName(rs.getString("PartName"));
                part.setStationCode(rs.getString("StationCode"));
                part.setMachineCode(rs.getString("MachineCode"));
                part.setControlPlanNo(rs.getString("ControlPlanNo"));
                parts.add(part);
            }
        }
        return parts;
    }

    public List<ParameterDTO> getParameters(String partCode) throws Exception {
        List<ParameterDTO> params = new ArrayList<>();
        String sql = "SELECT ParameterID, PartCode, Title, AcceptanceCriteria, ControlMethod, DisplayOrder FROM PM_Parameters WHERE PartCode = ? ORDER BY DisplayOrder ASC";
        try (Connection conn = getConnection();
             PreparedStatement stmt = conn.prepareStatement(sql)) {
            stmt.setString(1, partCode);
            try (ResultSet rs = stmt.executeQuery()) {
                while (rs.next()) {
                    ParameterDTO p = new ParameterDTO();
                    p.setParameterId(rs.getInt("ParameterID"));
                    p.setPartCode(rs.getString("PartCode"));
                    p.setTitle(rs.getString("Title"));
                    p.setAcceptanceCriteria(rs.getString("AcceptanceCriteria"));
                    p.setControlMethod(rs.getString("ControlMethod"));
                    p.setDisplayOrder(rs.getInt("DisplayOrder"));
                    params.add(p);
                }
            }
        }
        return params;
    }

    public boolean saveInspectionSession(String user, InspectionSubmissionDTO submission) throws Exception {
        String insertSessionSql = "INSERT INTO PM_InspectionSessions (PartCode, JiraIssueKey, InspectorUser, InspectionDateTime, Shift) VALUES (?, ?, ?, GETDATE(), ?)";
        String insertAnswerSql = "INSERT INTO PM_InspectionAnswers (SessionID, ParameterID, Sample1, Sample2, Sample3, Sample4, Sample5, FinalResult) VALUES (?, ?, ?, ?, ?, ?, ?, ?)";

        try (Connection conn = getConnection()) {
            conn.setAutoCommit(false);
            try (PreparedStatement sessionStmt = conn.prepareStatement(insertSessionSql, Statement.RETURN_GENERATED_KEYS)) {
                sessionStmt.setString(1, submission.getPartCode());
                sessionStmt.setString(2, submission.getJiraIssueKey());
                sessionStmt.setString(3, user);
                sessionStmt.setInt(4, submission.getShift());
                sessionStmt.executeUpdate();

                int sessionId;
                try (ResultSet rs = sessionStmt.getGeneratedKeys()) {
                    if (rs.next()) {
                        sessionId = rs.getInt(1);
                    } else {
                        throw new SQLException("Failed to retrieve generated SessionID");
                    }
                }

                try (PreparedStatement answerStmt = conn.prepareStatement(insertAnswerSql)) {
                    if (submission.getAnswers() != null) {
                        for (AnswerDTO ans : submission.getAnswers()) {
                            answerStmt.setInt(1, sessionId);
                            answerStmt.setInt(2, ans.getParameterId());
                            answerStmt.setString(3, ans.getSample1());
                            answerStmt.setString(4, ans.getSample2());
                            answerStmt.setString(5, ans.getSample3());
                            answerStmt.setString(6, ans.getSample4());
                            answerStmt.setString(7, ans.getSample5());
                            answerStmt.setString(8, ans.getFinalResult());
                            answerStmt.addBatch();
                        }
                        answerStmt.executeBatch();
                    }
                }
                conn.commit();
                return true;
            } catch (Exception e) {
                conn.rollback();
                throw e;
            } finally {
                conn.setAutoCommit(true);
            }
        }
    }
}
