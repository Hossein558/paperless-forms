package com.paperless.forms.servlet;

import com.atlassian.webresource.api.assembler.PageBuilderService;
import com.atlassian.templaterenderer.TemplateRenderer;

import com.atlassian.plugin.spring.scanner.annotation.imports.ComponentImport;

import jakarta.inject.Inject;
import jakarta.inject.Named;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServlet;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import java.io.IOException;
import java.util.HashMap;

@Named
public class PortalServlet extends HttpServlet {

    private final PageBuilderService pageBuilderService;
    private final TemplateRenderer templateRenderer;

    @Inject
    public PortalServlet(@ComponentImport PageBuilderService pageBuilderService, @ComponentImport TemplateRenderer templateRenderer) {
        this.pageBuilderService = pageBuilderService;
        this.templateRenderer = templateRenderer;
    }

    @Override
    protected void doGet(HttpServletRequest req, HttpServletResponse resp) throws ServletException, IOException {
        pageBuilderService.assembler().resources().requireWebResource("com.paperless.forms.paperless-forms-plugin:paperless-forms-plugin-resources");
        resp.setContentType("text/html;charset=utf-8");
        templateRenderer.render("templates/portal.vm", new HashMap<>(), resp.getWriter());
    }
}
