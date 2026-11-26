# hopo Android: choose, see and remove several files

Reported: on the phone, several files could not be sent; after choosing several, they were not shown and could not be removed.

- [x] Check on the phone whether the file picker returns several files.
- [x] Home Send panel: list every chosen file with its size and a remove button, plus Clear, scrolling when the list is long.
- [x] Android wording: no "Drop files here" on a phone.
- [x] Test on the phone: choose several files, remove one, send the rest to a PC.

## Result

- The phone's file picker already returned several files. The problem was the Home Send panel: it showed only one shortened line and had no way to remove a file.
- Tested on a Pixel 7a: chose 3 files, removed one, and sent 2 to a PC (2 files verified). Remove and Clear update the totals and the status line.
- 319 checks pass; CheckFormat passes.
