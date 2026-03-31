# MongoDB Exporter

This folder contains a small Python script to export all MongoDB databases and collections into files inside this folder.

Files:
- `export_all_mongo.py` - main exporter script. Exports each collection as a JSONL file and attempts a CSV export.
- `requirements.txt` - Python dependencies (pymongo).

Usage (PowerShell on Windows):

```powershell
# Create virtual env (optional)
python -m venv .venv; .\.venv\Scripts\Activate.ps1
pip install -r requirements.txt

# Run exporter (replace with your Mongo URI)
python export_all_mongo.py --uri "mongodb://username:password@hostname:27017" --outdir . --include-system
```

Notes:
- By default system databases (`admin`, `local`, `config`) are skipped. Use `--include-system` to include them.
- JSONL files preserve BSON types (ObjectId, datetimes) using `bson.json_util`.
- CSV export flattens nested documents; lists and dicts are JSON-serialized into CSV cells.
- Large collections are streamed so it should work with big datasets, but ensure you have disk space.

If you want an alternative output format or to stream to compressed files, tell me and I can add that.
