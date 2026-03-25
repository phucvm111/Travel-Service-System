// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

"use strict";

var con = new signalR.HubConnectionBuilder().withUrl("/hub").build();

con.on("loadAll", function () {
    var path = window.location.pathname.toLowerCase();
    var skipPages = ["/agents/tours/create", "/agents/tours/edit", "/users/profiles/edit", "admin/account/create", "agents/agentprofiles/editprofile", "/users/profiles/updatetourist"];
    var shouldSkip = skipPages.some(function (p) { return path.indexOf(p) !== -1; });
    if (!shouldSkip) {
        location.reload();
    }
});

con.start()
    .then()
    .catch(function (err) { return console.log(err.toString()); });