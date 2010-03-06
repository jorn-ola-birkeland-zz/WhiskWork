var crList = new Array();
var taskList = new Array();
var timer = null;

$(document).ready(function() {
	loadCrs();
	timer = setTimeout(reload, 1000*60);
});

function loadCrs() {
	$(document).bind('click', function(e) { 
		$('#crBox').hide(); 
		$("#liveEmanager").hide();
	});
	$("#liveEmanager").click(function(event){ event.stopPropagation();});
	$("#crBox").click(function(event){ event.stopPropagation();});
	$("#crBox").bind('dblclick', function() { showCr($('#box_id').html()); });

	$.getJSON("/",
		function(data) {
			for(var i in data) {
				addCrsToColumn(data[i]);
			}
		});


	$(".crlist").sortable({
			connectWith: ".crlist", 
			handle: "h3",
			placeholder: "space",
			forcePlaceholderSize: true,
			revert: true,
			items: ".cr",
			tolerance: "pointer",
			update: function(event, ui) {
				if (ui.sender != null && ui.sender.get(0) != this) {
					storeChange(ui.item, ui.sender.get(0), this);
				}				
			}
		}).disableSelection();
}

function reload() {
	$.getJSON("/",
		function(data) {
			updateCrs(data);
		});	
	clearTimeout(timer);
	timer = setTimeout(reload, 1000*60);
	checkStandup();
}




function updateCrs(worksteps) {
	var flatTaskWorkstepList = new Array();
	$("#scheduled .cr").remove().each(function(i) { crList[this.crdata.id] = null; });
	cleanUpOld(worksteps, crList);
	for(var i in worksteps) {
		for (var j in worksteps[i].workitemList) {
			if (crList[worksteps[i].workitemList[j].id] == null) {
				addCrToColumn($("#" + worksteps[i].workstep), worksteps[i].workitemList[j]);
			}
			crList[worksteps[i].workitemList[j].id].crdata = worksteps[i].workitemList[j];
			updateCrData(worksteps[i].workitemList[j].id);
			addAll(worksteps[i].workitemList[j].worksteps, flatTaskWorkstepList);
		}
	}	
	cleanUpOld(flatTaskWorkstepList, taskList);
	updateTasks(flatTaskWorkstepList);
}
function updateTasks(worksteps) {
	for(var i in worksteps) {
		for (var j in worksteps[i].workitemList) {
			if (taskList[worksteps[i].workitemList[j].id] == null) {
				addTaskToTaskColumn($("#" + worksteps[i].workstep), worksteps[i].workitemList[j]);
			}
		}
	}	
}
function cleanUpOld(worksteps, listOfElements) {
	var newListOfStatuses = new Array();
	for(var i in worksteps) {
		for(var j in worksteps[i].workitemList) {
			newListOfStatuses[worksteps[i].workitemList[j].id] = worksteps[i].workstep;
		}
	}
	for(var i in listOfElements) {
		if (listOfElements[i] != null && (newListOfStatuses[i] == null || newListOfStatuses[i] != listOfElements[i].parentNode.id)) {
			$(listOfElements[i]).remove()
			listOfElements[i] = null;
		}
	}
}
function addAll(arrayFrom, arrayTo) {
	for (var i in arrayFrom) {
		arrayTo.push(arrayFrom[i]);
	}
}


function hasOpenTasks(cr) {
	return cr.find(".innerTaskColumn.new .task").size() > 0 || cr.find(".innerTaskColumn.inprocess .task").size() > 0;
}


function storeChange(cr, fromColumn, toColumn) {
	save(toColumn.id, {id: cr.get(0).crdata.id});
	reload();
}

function save(columnId, data) {
	$.post("/" + columnId.replace("-", "/"), data, "json");		
}


function addCrsToColumn(columnData) {
	var column = $("#" + columnData.workstep);
	for (var i in columnData.workitemList) {
		addCrToColumn(column, columnData.workitemList[i])
	}
}

function getWorkstep(cr) {
	return $(cr).parent().attr("id");
}


// I need refactoring
function addCrToColumn(column, crdata) {
	var cr = $("<div>").attr("id", "cr" + crdata.id).addClass("cr").appendTo(column);
	if (ar = /.*-(.*)/.exec(crdata.id)) {
		cr.addClass("under" + ar[1]);
	}
	crList[crdata.id] = cr.get(0);
	crList[crdata.id].crdata = crdata;
	var crInner = $("<div>").addClass("crInner").appendTo(cr);
	$("<h3>").appendTo(crInner).text(crdata.id);
	$("<div>").addClass("title").appendTo(crInner);
	$("<div>").addClass("name").appendTo(crInner);
	updateCrData(crdata.id);
	var taskList = $("<div>").addClass("tasks").appendTo(cr);
	$("<h3>").html("Tasks").appendTo(taskList);
	$("<div>").addClass("new").addClass("innerTaskColumn").appendTo(taskList).attr("id", "development-inprocess-" + crdata.id + "-tasks-new");
	$("<div>").addClass("inprocess").addClass("innerTaskColumn").appendTo(taskList).attr("id", "development-inprocess-" + crdata.id + "-tasks-inprocess");
	$("<div>").addClass("done").addClass("innerTaskColumn").appendTo(taskList).attr("id", "development-inprocess-" + crdata.id + "-tasks-done");
	$(taskList).children(".innerTaskColumn").sortable({
			connectWith: "#cr" + crdata.id + " .innerTaskColumn",
			placeholder: "space",
			forcePlaceholderSize: true,
			revert: true,
			cursor: "move",
			update: function(event, ui) {
				if (ui.sender != null && ui.sender.get(0) != this) {
					storeTaskChange(ui.item, ui.sender.get(0), this);
				}
			}
		}).disableSelection();
	if (column.hasClass("inprocess") && column.parent().get(0) == $("#development").get(0)) {
		for (var i in crdata.worksteps) {
			var taskColumn = $(taskList).children("#" + crdata.worksteps[i].workstep);
			for (var j in crdata.worksteps[i].workitemList) {		
				addTaskToTaskColumn(taskColumn, crdata.worksteps[i].workitemList[j]);
			}		
			
		}
	}
	cr.bind('dblclick', function(e) { showCrData(this); });
}

function updateCrData(crid) {
	var cr = crList[crid];
	$(cr).find(".title").html(emptyIfNull(cr.crdata.title));
	$(cr).find(".name").html(emptyIfNull(cr.crdata.responsible));
}


function showCrData(cr) {
	var crjq = $(cr).children(".crInner");
	$('#box_id').html(cr.crdata.id);
	$('#box_title').html(cr.crdata.title);
	$('#box_release').html(cr.crdata.release);
	$('#box_responsible').val(cr.crdata.responsible);
	var left = crjq.offset().left;
	if (left + $('#crBox').width() > jQuery(window).width()) {
		left = jQuery(window).width() - $('#crBox').width();
	} 
	$('#crBox').show().css({top: crjq.offset().top, left: left});
}

function storeTaskChange(task, fromColumn, toColumn) {
	alert("Moving task " + task.get(0).taskdata.id + " from " + fromColumn.id + " to " + toColumn.id);
	var cr = $(toColumn).parents(".cr");
	if (hasOpenTasks(cr)) {
		cr.addClass("hasOpenTasks");
	} else {
		cr.removeClass("hasOpenTasks");
	}
	cr.parent().sortable('option', 'items',  ".cr:not(.hasOpenTasks)");
}


function addTaskToTaskColumn(taskColumn, taskdata) {
	var task = $("<div>").text(taskdata.id).addClass("task").appendTo(taskColumn);
	taskList[taskdata.id] = task.get(0);
	taskList[taskdata.id].taskdata = taskdata;
}

function saveResponsible(responsible, crId) {
	save(getWorkstep(crList[crId]), {id: crId, responsible: responsible});
	reload();
}

function emptyIfNull(value) {
	if (value != null) return value;
	return "";
}

function showCr(crId) {
	var unid = crList[crId].crdata.unid;
	$("#liveEmanager").show().attr("src", "http://bluestone.bekk.no/global/seitp/seitp210.ns6/0/" + unid);
}


function checkStandup() {
	var now = new Date();
	if (now.getHours() == 9 && now.getMinutes() == 0) {
		var standup = $("#standup").width($(window).width()).height($(window).height()).show();
		standup.click(function(e) { standup.hide(); });
	}
}






