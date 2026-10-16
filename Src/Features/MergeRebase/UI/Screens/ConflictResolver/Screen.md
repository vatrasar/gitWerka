# ConflictResolver Screen

## Purpose
A 3-panel merge conflict resolution screen inspired by GitKraken. Enables users to visually inspect conflicts between local (Ours) and remote (Theirs) branches, select chunks via checkboxes, view live output, and save/stage the resolved file.

## Functionalities
- Side-by-side inspection of Panel A (Local/Ours) and Panel B (Remote/Theirs).
- Checkbox selection per conflict hunk to take Ours, Theirs, or both.
- "Take All Ours" and "Take All Theirs" quick actions.
- Live reactive Output panel showing the merged result with line numbers and purple conflict highlights.
- Prev/Next conflict navigation.
- "Save" action writing clean content to disk and staging in the Git index.
- Back / Cancel action restoring to CommitLog.

## Key UI Elements
- `BackButton`: Return to previous screen.
- `SaveButton`: Save resolved file and stage it.
- `TakeAllOursButton`, `TakeAllTheirsButton`: Quick resolution buttons.
- `OursAllCheckBox`, `TheirsAllCheckBox`: Header-level select all checkboxes.
- `OursLinesItemsControl`, `TheirsLinesItemsControl`: Monospace code views with checkboxes per conflict hunk.
- `OutputLinesItemsControl`: Merged code view with line numbers.
- `PreviousConflictButton`, `NextConflictButton`: Conflict navigation buttons.
