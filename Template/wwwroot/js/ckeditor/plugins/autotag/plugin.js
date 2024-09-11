

// Register the plugin in the editor.
CKEDITOR.plugins.add('autotag', {
    requires: 'autocomplete,textmatch',

    init: function (editor) {
        editor.on('instanceReady', function () {
            var config = {};
            // Called when the user types in the editor or moves the caret.
            // The range represents the caret position.
            function textTestCallback(range) {
                // You do not want to autocomplete a non-empty selection.
                if (!range.collapsed) {
                    return null;
                }

                // Use the text match plugin which does the tricky job of performing
                // a text search in the DOM. The "matchCallback" function should return
                // a matching fragment of the text.
                return CKEDITOR.plugins.textMatch.match(range, matchCallback);
            }

            // Returns the position of the matching text.
            // It matches a word starting from the '#' character
            // up to the caret position.
            function matchCallback(text, offset) {
                console.log(text, offset);
                // Get the text before the caret.
                var left = text.slice(0, offset),
                    // Will look for a '#' character followed by a ticket number.
                    match = left.match(/#\w*$/);

                if (!match) {
                    return null;
                }
                console.log(match);

                return {
                    start: match.index,
                    end: offset
                };
            }

            config.textTestCallback = textTestCallback;

            // The itemsArray variable is the example "database".
            var itemsArray = [
                {
                    id: 337,
                    name: '${CANDIDATE ID}',
                    type: 'feature'
                },
                {
                    id: 440,
                    name: '${Employee ID/Code}',
                    type: 'bug'
                },
                {
                    id: 468,
                    name: '${EMP First Name}',
                    type: 'task'
                },
                {
                    id: 558,
                    name: '${EMP Middle Name}',
                    type: 'feature'
                },
                {
                    id: 566,
                    name: '${EMP Last Name}',
                    type: 'bug',
                },
                {
                    id: 584,
                    name: '${Letter Date}',
                    type: 'feature'
                },
                {
                    id: 616,
                    name: '${Location}',
                    type: 'feature'
                },
                {
                    id: 648,
                    name: '${RCS Grade}',
                    type: 'bug'
                },
                {
                    id: 740,
                    name: '${Designation}',
                    type: 'feature'
                },
                {
                    id: 786,
                    name: '${Rating}',
                    type: 'task'
                },
                {
                    id: 856,
                    name: '${Service Period}',
                    type: 'feature'
                },
                {
                    id: 859,
                    name: '${DOJ}',
                    type: 'bug'
                },
                {
                    id: 932,
                    name: '${DOC}',
                    type: 'feature'
                },
                {
                    id: 933,
                    name: '${DOR}',
                    type: 'feature'
                },
                {
                    id: 1010,
                    name: '${DOL}',
                    type: 'bug'
                },
                {
                    id: 1529,
                    name: '${RM Code}',
                    type: 'bug'
                },
                {
                    id: 1530,
                    name: '${RM Name}',
                    type: 'feature'
                },
                {
                    id: 1570,
                    name: '${Employee Category}',
                    type: 'bug'
                },
                {
                    id: 1703,
                    name: '${Effective Date}',
                    type: 'feature'
                },
                {
                    id: 1746,
                    name: '${CTC}',
                    type: 'feature'
                },
                {
                    id: 1751,
                    name: '${Old CTC}',
                    type: 'feature'
                },
                {
                    id: 1776,
                    name: '${New CTC}',
                    type: 'bug'
                },
                {
                    id: 1993,
                    name: '${Old Basic}',
                    type: 'feature'
                },
                {
                    id: 2062,
                    name: '${New Basic}',
                    type: 'feature'
                },
                {
                    id: 2063,
                    name: '${Old HRA}',
                    type: 'feature'
                },
                {
                    id: 2064,
                    name: '${New HRA}',
                    type: 'feature'
                },
                {
                    id: 2065,
                    name: '${Old Special}',
                    type: 'feature'
                },
                {
                    id: 2066,
                    name: '${New Special}',
                    type: 'feature'
                },
                {
                    id: 2067,
                    name: '${Old PF}',
                    type: 'feature'
                },
                {
                    id: 2068,
                    name: '${Old CTC per month}',
                    type: 'feature'
                },
                {
                    id: 2069,
                    name: '${New CTC per month}',
                    type: 'feature'
                },
                {
                    id: 2070,
                    name: '${Old Incentive}',
                    type: 'feature'
                },
                {
                    id: 2071,
                    name: '${New Incentive}',
                    type: 'feature'
                },
                {
                    id: 2072,
                    name: '${Old Total CTC}',
                    type: 'feature'
                },
                {
                    id: 2073,
                    name: '${New Total CTC}',
                    type: 'feature'
                },
                {
                    id: 2074,
                    name: '${New Monthly Gross Salary}',
                    type: 'feature'
                }
            ];
            // Returns (through its callback) the suggestions for the current query.
            function dataCallback(matchInfo, callback) {
                // Remove the '#' tag.
                var query = matchInfo.query.substring(1);
                // Simple search.
                // Filter the entire items array so only the items that start
                // with the query remain.
                var suggestions = itemsArray.filter(function (item) {
                    return String(item.name.replace("${", "").replace("}", "")).indexOf(query) == 0;
                });

                // Note: The callback function can also be executed asynchronously
                // so dataCallback can do an XHR request or use any other asynchronous API.
                callback(suggestions);
            }

            config.dataCallback = dataCallback;

            // Define the templates of the autocomplete suggestions dropdown and output text.
            config.itemTemplate = '<li data-id="{id}" class="issue-{type}">{name}</li>';

            config.outputTemplate = '{name}';


            // Attach autocomplete to the editor.
            new CKEDITOR.plugins.autocomplete(editor, config);
        });
    }
});

