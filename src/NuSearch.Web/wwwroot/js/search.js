$(function() {

  $("select").change(function() { $("form#search-criteria").submit(); });
  $("input[type='checkbox']").change(function() { $("form#search-criteria").submit(); });
  jQuery.timeago.settings.cutoff = 1000*60*60*24*365; // show regular date if more than 1 year in the past
  $(".timeago").timeago();

  setupTypeAhead();

  function setupTypeAhead() {
    var typeAheadOptions = {
      hint: true,
      highlight: true,
      minLength: 1
    };

    var remoteHandler = function(query, process) {
      return $.ajax(
          {
            cache: false,
            type: "POST",
            url: "/suggest",
            data: JSON.stringify({ Query: query }),
            contentType: "application/json; charset=utf-8",
            dataType: "json"
          })
        .success(function(suggestions) { process(suggestions); });
    };

    $('#query').typeahead(typeAheadOptions,
      {
        displayKey: "id",
        templates: {
          empty: [
            '<div class="lead">',
            'no suggestions found for current prefix',
            '</div>'
          ].join('\n'),
          suggestion: function(suggestion) {
            return [
              '<h4 class="text-primary">',
              suggestion.id,
              '<span class="text-humble pull-right">',
              suggestion.downloadCount + " downloads",
              '</span>',
              '</h5>',
              '<h5 class="text-primary">',
              suggestion.summary,
              '</h6>'
            ].join('\n');
          }
        },
        source: remoteHandler
      }
    )
    .on('typeahead:selected', function(e, o) {
      $("#query").val(o.id);
      $("form#search-criteria").submit();
    });
  }

  $("#query").focus().select();

});
