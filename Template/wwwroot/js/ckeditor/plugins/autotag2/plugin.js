CKEDITOR.plugins.add('autotag2', {
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
                    match = left.match(/@\w*$/);

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
                    name: '<table align="center" border="1" cellpadding="1" cellspacing="1"><tbody><tr><td>&nbsp; <strong>Pay Elements</strong></td><td><strong>MONTHLY</strong></td><td><strong>ANNUAL</strong></td></tr><tr><td>Basic</td><td>${Monthly Basic}</td><td>${New Basic}</td></tr><tr><td>Flexi Benefit</td><td>${Monthly Special}</td><td>${New Special}</td></tr><tr><td>Annual Wage Supplement</td><td>${Annual Wage Supplement Monthly}</td><td>${Annual Wage Supplement}</td></tr><tr><td><strong>Annual Base Salary (Total Guaranteed Payment)</strong></td><td><strong>${Annual Base Salary Monthly}</strong></td><td><strong>${Annual Base Salary}</strong></td></tr></tbody></table>',
                    type: 'Salary'
                },
                {
                    id: 440,
                    name: '<table align="center" border="1" cellpadding="1" cellspacing="1"><tbody><tr><td>&nbsp; <strong>Pay Elements</strong></td><td><strong>MONTHLY</strong></td><td><strong>ANNUAL</strong></td></tr><tr><td>Basic</td><td>${Monthly Basic}</td><td>${New Basic}</td></tr><tr><td>Flexi Benefit</td><td>${Monthly Special}</td><td>${New Special}</td></tr><tr><td>Annual Wage Supplement</td><td>${Annual Wage Supplement Monthly}</td><td>${Annual Wage Supplement}</td></tr><tr><td><strong>Annual Base Salary (Total Guaranteed Payment)</strong></td><td><strong>${Annual Base Salary Monthly}</strong></td><td><strong>${Annual Base Salary}</strong></td></tr></tbody></table>',
                    type: 'ANNEXURE'
                }
            ];

            // Returns (through its callback) the suggestions for the current query.
            function dataCallback(matchInfo, callback) {
                // Remove the '#' tag.
                var query = matchInfo.query.substring(1);
                console.log(query);
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
            config.itemTemplate = '<li data-id="{id}" class="issue-{type}">{type}</li>';
            config.outputTemplate = function (item) {
                // Parse and insert the HTML content into the editor's content.
                var parser = new DOMParser();
                var doc = parser.parseFromString(item.name, 'text/html');
                var content = doc.body.textContent;
                editor.insertHtml(content);
            };
            // Attach autocomplete to the editor.
            new CKEDITOR.plugins.autocomplete(editor, config);
        });
    }
});
