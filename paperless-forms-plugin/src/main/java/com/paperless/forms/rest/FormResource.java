package com.paperless.forms.rest;

import com.paperless.forms.rest.dto.FormDTO;
import com.paperless.forms.service.DatabaseService;

import jakarta.ws.rs.GET;
import jakarta.ws.rs.Path;
import jakarta.ws.rs.Produces;
import jakarta.ws.rs.core.MediaType;
import jakarta.ws.rs.core.Response;
import java.util.List;

import javax.inject.Inject;
import javax.inject.Named;

@Path("/forms")
@Named
public class FormResource {

    private final DatabaseService databaseService;

    @Inject
    public FormResource(DatabaseService databaseService) {
        this.databaseService = databaseService;
    }

    @GET
    @Produces("application/json; charset=UTF-8")
    public Response getForms() {
        try {
            List<FormDTO> forms = databaseService.getForms();
            return Response.ok(forms).build();
        } catch (Exception e) {
            e.printStackTrace();
            return Response.serverError().entity("{\"error\": \"" + e.getMessage() + "\"}").build();
        }
    }
}
