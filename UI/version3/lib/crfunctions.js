var crList = new Array();
var taskList = new Array();
var timer = null;
var loaderimage;
var currentRelease = "20100213";


$(document).ready(function() {
	loadCrs();
	timer = setTimeout(reload, 1000*60);
	checkStandup();
	loaderimage = $('<img src="images/ajax-loader.gif">').css({position: "absolute", zIndex: 20000});
	loaderimage.appendTo(document.body).hide();
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

function showLoader() {
	loaderimage.css({
		left: (jQuery(window).width() - loaderimage.width())/2,
		top: (jQuery(window).height() - loaderimage.height())/2}).show();
}

function reload() {
	showLoader();
	$.getJSON("/",
		function(data) {
			loaderimage.hide();
			updateCrs(data);
		});	
	clearTimeout(timer);
	timer = setTimeout(reload, 1000*60);
}
function reloadAfterPost(data) {
	reload();
}



function updateCrs(worksteps) {
	var flatTaskWorkstepList = new Array();
	$("#cmsanalysis-scheduled .cr, #cmsdev-scheduled .cr").remove().each(function(i) { crList[this.crdata.id] = null; });
	cleanUpOld(worksteps, crList);
	for(var i in worksteps) {
		for (var j in worksteps[i].workitemList) {
			if (crList[worksteps[i].workitemList[j].id] == null) {
				addCrToColumn($("#" + worksteps[i].workstep), worksteps[i].workitemList[j]);
			}
			if (crList[worksteps[i].workitemList[j].id] != null) {
				crList[worksteps[i].workitemList[j].id].crdata = worksteps[i].workitemList[j];
				updateCrData(worksteps[i].workitemList[j].id);
				addAll(worksteps[i].workitemList[j].worksteps, flatTaskWorkstepList);
			}
		}
	}	
	cleanUpOld(flatTaskWorkstepList, taskList);
	updateTasks(flatTaskWorkstepList);
	for(var i in worksteps) {
		setupStacking(worksteps[i].workstep);
	}
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
			var elm = $(listOfElements[i]); 
			elm.remove();
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
}

function save(columnId, data) {
	showLoader();
	$.post("/" + columnId.replace(/-/g, "/"), data, reloadAfterPost);		
}


function addCrsToColumn(columnData) {
	var column = $("#" + columnData.workstep);
	for (var i in columnData.workitemList) {
		addCrToColumn(column, columnData.workitemList[i])
	}
	setupStacking(columnData.workstep);
}

function setupStacking(columnId) {
	if (columnId.indexOf("scheduled") == -1) {
		var column = $("#" + columnId);
		if (column.children(".crlist .cr").size() > 8) {
			column.addClass("stacked");
		} else {	
			column.removeClass("stacked");
		}
	}
}


function getWorkstep(cr) {
	return $(cr).parent().attr("id");
}


// I need refactoring
function addCrToColumn(column, crdata) {
	if (column.length == 0) return;
	var cr = $("<div>").attr("id", "cr" + crdata.id).addClass("cr").appendTo(column);
	if (crdata.age != null && crdata.age != "") {
		cr.addClass(crdata.age)
	}
	crdata.isNextRelease = (crdata.release != null && crdata.release != "" && crdata.release > currentRelease);
	if (crdata.isNextRelease) {
		cr.addClass("nextRelease");
	}
	
	if (crdata.type != null && crdata.type == "bug") {
		cr.addClass("bug");
	}
	
	if (ar = /.*-(.*)/.exec(crdata.id)) {
		cr.addClass("under" + ar[1]);
	}
	crList[crdata.id] = cr.get(0);
	crList[crdata.id].crdata = crdata;
	var crInner = $("<div>").addClass("crInner").addClass(crdata.team).appendTo(cr);
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
	if (cr.crdata.isNextRelease) {
		$("#crBox").addClass("nextRelease");
	} else {
		$("#crBox").removeClass("nextRelease");	
	}
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
	if (document.location.href.indexOf("smartboard1.bekk.no/") == -1) return;
	var now = new Date();
	if (now.getHours() == 9 && now.getMinutes() == 0) {
		showStandup();
	}
	setTimeout(checkStandup, 60*1000);
}
function showStandup() {
	var standup = $("#standup").width($(window).width()).height($(window).height() + 100).show();
	standup.click(function(e) { standup.hide(); });
	playMusic();
	setTimeout(function() { standup.hide(); }, 15*60*1000);
}

function playMusic() {
	if (document.location.href.indexOf("bekk-smartboard:") > -1) return;
	var soundFile = "http://bekk-smartboard/sounds/reveille.mp3";
	$('<OBJECT ID="objMediaPlayer" WIDTH="0" HEIGHT="0" CLASSID="CLSID:22D6F312-B0F6-11D0-94AB-0080C74C7E95" STANDBY="Loading Windows Media Player components..." TYPE="application/x-oleobject">' + 
		'<PARAM NAME="FileName" VALUE="' + soundFile + '">' +
		'<EMBED TYPE="application/x-mplayer2" SRC="' + soundFile + '" autostart="true" loop="false" NAME="objMediaPlayer" WIDTH="0" HEIGHT="0"></EMBED></OBJECT>').appendTo($(document.body));
}



