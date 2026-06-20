package com.paperless.forms.rest;

import com.atlassian.jira.component.ComponentAccessor;
import com.atlassian.jira.user.ApplicationUser;
import com.paperless.forms.rest.dto.InspectionSubmissionDTO;
import com.paperless.forms.service.DatabaseService;

import jakarta.inject.Inject;
import jakarta.ws.rs.*;
import jakarta.ws.rs.core.MediaType;
import jakarta.ws.rs.core.Response;

@Path("/ipi")
public class InspectionResource {

    private final DatabaseService databaseService;

    @Inject
    public InspectionResource(DatabaseService databaseService) {
        this.databaseService = databaseService;
    }

    @GET
    @Path("/parts")
    @Produces(MediaType.APPLICATION_JSON)
    public Response getParts() {
        try {
            return Response.ok(databaseService.getParts()).build();
        } catch (Exception e) {
            e.printStackTrace();
            return Response.serverError().entity("Error fetching parts: " + e.getMessage()).build();
        }
    }

    @GET
    @Path("/parameters")
    @Produces(MediaType.APPLICATION_JSON)
    public Response getParameters(@QueryParam("partCode") String partCode) {
        try {
            if (partCode == null || partCode.isEmpty()) {
                return Response.status(Response.Status.BAD_REQUEST).entity("partCode is required").build();
            }
            return Response.ok(databaseService.getParameters(partCode)).build();
        } catch (Exception e) {
            e.printStackTrace();
            return Response.serverError().entity("Error fetching parameters: " + e.getMessage()).build();
        }
    }

    @POST
    @Path("/sessions")
    @Consumes(MediaType.APPLICATION_JSON)
    @Produces(MediaType.APPLICATION_JSON)
    public Response saveSession(InspectionSubmissionDTO submission) {
        try {
            ApplicationUser user = ComponentAccessor.getJiraAuthenticationContext().getLoggedInUser();
            String username = (user != null) ? user.getUsername() : "anonymous";
            
            boolean success = databaseService.saveInspectionSession(username, submission);
            if (success) {
                return Response.ok("{\"status\":\"success\"}").build();
            } else {
                return Response.serverError().entity("Failed to save session").build();
            }
        } catch (Exception e) {
            e.printStackTrace();
            return Response.serverError().entity("Error saving session: " + e.getMessage()).build();
        }
    }
}
