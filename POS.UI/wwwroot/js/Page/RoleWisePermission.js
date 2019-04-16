﻿const rolewisePermission = (() => {
    //********* Private Variables **************//
    let menuTreeView = $("#MenuTreeView");


    //********* Private Methos *****************//
    let init = () => {
        //load all menu first
        LoadTreeViewDefault();

        //Load Rolewise menu present
        if (roleWiseMenuPermissionData !== null || roleWiseMenuPermissionData !== undefined)
            UpdateTreeViewWithSelectedData(roleWiseMenuPermissionData);

        //Load Rolewise User Rights
        if (GetUrlParameters() !== 0 && GetUrlParameters() !== "RoleWisePermission") {
            if ($("#roleWiseUserPermission_RoleId").val() === "")
                $("#roleWiseUserPermission_RoleId").val(GetUrlParameters());
        }


    };
    let GetTreeViewMenuData = (callback) => {
        $.ajax({
            method: "GET",
            url: "/Settings/GetMenu",
            complete: function (result) {
                if (result.status === 200) {
                    callback(result.responseJSON);
                }
            }
        });
    };
    let ConvertDataToTreeviewFormat = (list) => {
        var map = {}, node, roots = [], i;
        for (i = 0; i < list.length; i += 1) {
            list[i].text = list[i].name;
            list[i].menuId = list[i].id;
            map[list[i].id] = i; // initialize the map

            list[i].nodes = []; // initialize the children
        }
        for (i = 0; i < list.length; i += 1) {
            node = list[i];
            if (node.parentId === "0" || node.parentId === null || node.parentId === undefined) {
                roots.push(node);
            } else {
                // if you have dangling branches check that map[node.parentId] exists
                list[map[node.parentId]].nodes.push(node);
            }
        }
        return roots;
    };
    let LoadTreeViewDefault = () => {
        GetTreeViewMenuData((result) => {
            var formattedData = ConvertDataToTreeviewFormat(result);

            menuTreeView.treeview({
                data: formattedData,

                // custom icons
                expandIcon: 'fa fa-plus',
                collapseIcon: 'fa fa-minus',
                emptyIcon: 'glyphicon',
                nodeIcon: '',
                selectedIcon: '',
                checkedIcon: 'fa fa-check-square',
                uncheckedIcon: 'fa fa-square',
                // shows borders
                showBorder: true,
                // shows icons
                showIcon: true,
                // shows checkboxes
                showCheckbox: false,
                // shows tags
                showTags: false,
                // enables multi select
                multiSelect: true

            });
        });
    };
    let UpdateTreeViewWithSelectedData = (treeviewData) => {
        if (!$.isArray(menuTreeView.treeview('getUnselected', [])))
            setTimeout(() => { UpdateTreeViewWithSelectedData(treeviewData); });
        else {
            var getAllNodes = menuTreeView.treeview('getUnselected', []);
            var selectedNode = _.filter(getAllNodes, function (x) {
                return _.find(treeviewData, function (y) { return y.MenuId == x.id });
            });
            menuTreeView.treeview('selectNode', [selectedNode, { silent: true }]);
        }
    };
    let SavePermission = () => {
        var nodeId = [];
        var selectedNodes = menuTreeView.treeview('getSelected', nodeId);
        var selectedNodesFinal = [];
        _.each(selectedNodes, function (val) {
            selectedNodesFinal.push({
                RoleId: $("#roleWiseUserPermission_RoleId").val(),
                MenuId: val.id
            });
        });
        //getting data
        let data = {
            roleWiseUserPermission: {

                RoleId: $("#roleWiseUserPermission_RoleId").val(),
                Sales_Discount_Flat_Item: $("#roleWiseUserPermission_Sales_Discount_Flat_Item").is(":checked"),
                Sales_Discount_Line_Item: $("#roleWiseUserPermission_Sales_Discount_Line_Item").is(":checked"),
                Sales_Discount_Flat_Item_Limit: $("#roleWiseUserPermission_Sales_Discount_Flat_Item_Limit").val(),
                Sales_Discount_Line_Item_Limit: $("#roleWiseUserPermission_Sales_Discount_Line_Item_Limit").val(),
                Sales_Rate_Edit: $("#roleWiseUserPermission_Sales_Rate_Edit").is(":checked")
            },
            roleWiseMenuPermissions: selectedNodesFinal
        };
        $.ajax({
            method: "POST",
            url: "/Account/RoleWisePermission",
            dataType: "json",
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify(data),
            complete: function (result) {

                if (result.status === 200) {
                    window.location.href = window.location.origin + "/Account/RoleWisePermission/" + result.responseText;
                }
            }
        });
    };


    //********* Events ************************//
    $('#roleWiseUserPermission_Sales_Discount_Line_Item').click(function () {

        if ($('#roleWiseUserPermission_Sales_Discount_Line_Item').is(":checked") == true)
            $("#roleWiseUserPermission_Sales_Discount_Line_Item_Limit").attr("disabled", false);
        else
            $("#roleWiseUserPermission_Sales_Discount_Line_Item_Limit").attr("disabled", true);
    });
    $('#roleWiseUserPermission_Sales_Discount_Flat_Item').click(function () {

        if ($('#roleWiseUserPermission_Sales_Discount_Flat_Item').is(":checked") == true)
            $("#roleWiseUserPermission_Sales_Discount_Flat_Item_Limit").attr("disabled", false);
        else
            $("#roleWiseUserPermission_Sales_Discount_Flat_Item_Limit").attr("disabled", true);
    });
    $("#roleWiseUserPermission_RoleId").change(function () {
        var roleId = $("#roleWiseUserPermission_RoleId").val();
        window.location.href = window.location.origin + "/Account/RoleWisePermission/" + roleId;
    });
    $("#Save").on('click', SavePermission);



    //********* Public Output ****************//
    return {
        init
    };

})().init();