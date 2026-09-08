# Planning-phase generators

These four scripts produced `content/`, `schema/`, `manifest/` and
`docs/CONTENT_SCHEMA.md` during the design phase. They are authoring aids, not game
code, and they are kept so that the generated files can be regenerated and re-validated
rather than drifting.

```
pip install jsonschema
python3 tools/planning/gen_content.py    # content/*.json (referential checks; exits 1 on error)
python3 tools/planning/gen_schema.py     # schema/content.schema.json, then validates every content file
python3 tools/planning/gen_manifest.py   # manifest/sprites.json, greybox_palette.json, schema/manifest.schema.json
python3 tools/planning/gen_doc.py        # docs/CONTENT_SCHEMA.md with verbatim excerpts
```

Run them in that order. All four are deterministic; running them on a clean checkout
produces no diff. Once `packages/content` and `packages/manifest` exist (ROADMAP M0),
their validators supersede the checks here and these scripts become the authoring
source for the catalogue only.
