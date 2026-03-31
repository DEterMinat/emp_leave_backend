"""Export all MongoDB databases/collections to JSONL and CSV files.

- Writes one newline-delimited JSON file per collection (collection.jsonl).
- Tries to write a CSV per collection (collection.csv) by flattening documents; nested structures become JSON strings.

Usage:
    python export_all_mongo.py --uri "mongodb://user:pass@host:27017" --outdir ./ --include-system

Defaults:
    outdir: Python_Export_Mongo (the repo folder)
    excludes system DBs: admin, local, config (unless --include-system)

Dependencies: pymongo
"""
from __future__ import annotations
import argparse
import csv
import json
import os
import sys
from typing import Dict, Any, Iterable, List

# Import pymongo and bson with a friendly error if the package is missing
try:
    from pymongo import MongoClient
    from bson import json_util
except Exception as e:  # ImportError or other import-time issues
    print("Error: required package 'pymongo' (and 'bson') is not installed or cannot be imported.")
    print("Please install dependencies with: pip install -r requirements.txt")
    print(f"Import error details: {e}")
    sys.exit(1)

SYSTEM_DBS = {"admin", "local", "config"}


def ensure_dir(path: str) -> None:
    os.makedirs(path, exist_ok=True)


def flatten_doc(d: Dict[str, Any], parent_key: str = "", sep: str = ".") -> Dict[str, Any]:
    """Flatten nested dict to a single-level dict with dot-separated keys.
    Lists and non-dict values are left as-is (lists will be JSON-serialized when writing CSV).
    """
    items: Dict[str, Any] = {}
    for k, v in d.items():
        new_key = f"{parent_key}{sep}{k}" if parent_key else k
        if isinstance(v, dict):
            items.update(flatten_doc(v, new_key, sep=sep))
        else:
            items[new_key] = v
    return items


def export_collection_jsonl(collection, out_path: str) -> int:
    """Export a pymongo Collection to a JSONL file. Returns number of documents written."""
    written = 0
    with open(out_path, "w", encoding="utf-8") as f:
        for doc in collection.find({}):
            # Use bson.json_util to preserve ObjectId, datetimes, etc.
            f.write(json_util.dumps(doc))
            f.write("\n")
            written += 1
    return written


def export_collection_csv(collection, out_path: str, sample_size: int = 100) -> int:
    """Export a pymongo Collection to CSV by flattening documents.
    Collect header fields from a sample of documents then stream all documents.
    Returns number of rows written.
    """
    # Sample some docs to discover fields
    cursor = collection.find({}).limit(sample_size)
    headers = set()
    sample_docs = []
    for doc in cursor:
        flat = flatten_doc(doc)
        # Convert keys to strings
        headers.update(flat.keys())
        sample_docs.append(flat)

    headers_list = sorted(headers)
    if not headers_list:
        return 0

    # Stream all docs and write CSV
    written = 0
    with open(out_path, "w", encoding="utf-8", newline="") as f:
        writer = csv.writer(f)
        writer.writerow(headers_list)
        for doc in collection.find({}):
            flat = flatten_doc(doc)
            row = []
            for h in headers_list:
                v = flat.get(h, "")
                # For nested lists/dicts, serialize to JSON string
                if isinstance(v, (dict, list)):
                    row.append(json.dumps(v, default=str, ensure_ascii=False))
                else:
                    # json_util cannot be used directly for simple values here; convert to str for safety
                    row.append(v if v is not None else "")
            writer.writerow(row)
            written += 1
    return written


def export_db(client: MongoClient, db_name: str, outdir: str, include_system: bool = False) -> Dict[str, Any]:
    report = {"db": db_name, "collections": []}
    if not include_system and db_name in SYSTEM_DBS:
        return report

    db = client[db_name]
    col_names = db.list_collection_names()
    db_dir = os.path.join(outdir, db_name)
    ensure_dir(db_dir)

    for col in col_names:
        collection = db[col]
        safe_col_name = col.replace("/", "_")
        jsonl_path = os.path.join(db_dir, f"{safe_col_name}.jsonl")
        csv_path = os.path.join(db_dir, f"{safe_col_name}.csv")

        print(f"Exporting {db_name}.{col} -> {jsonl_path}")
        count_jsonl = export_collection_jsonl(collection, jsonl_path)
        print(f"  Wrote {count_jsonl} documents to JSONL")

        try:
            print(f"  Attempting CSV export to {csv_path}")
            count_csv = export_collection_csv(collection, csv_path)
            print(f"  Wrote {count_csv} rows to CSV")
        except Exception as e:
            print(f"  Skipped CSV for {db_name}.{col} due to error: {e}")
            count_csv = 0

        report["collections"].append({"name": col, "jsonl": count_jsonl, "csv": count_csv})

    return report


def main() -> None:
    parser = argparse.ArgumentParser(description="Export all MongoDB databases and collections")
    parser.add_argument("--uri", required=True, help="MongoDB connection URI (e.g. mongodb://user:pass@host:27017)")
    parser.add_argument("--outdir", default=os.path.join(os.getcwd(), "Python_Export_Mongo"), help="Output directory")
    parser.add_argument("--include-system", action="store_true", help="Include system databases (admin, local, config)")
    args = parser.parse_args()

    outdir = os.path.abspath(args.outdir)
    ensure_dir(outdir)

    print(f"Connecting to MongoDB at: {args.uri}")
    client = MongoClient(args.uri)

    db_names = client.list_database_names()
    print(f"Found databases: {db_names}")

    master_report = {"databases": []}
    for db_name in db_names:
        print(f"Processing database: {db_name}")
        report = export_db(client, db_name, outdir, include_system=args.include_system)
        master_report["databases"].append(report)

    # Write a summary report
    summary_path = os.path.join(outdir, "export_summary.json")
    with open(summary_path, "w", encoding="utf-8") as f:
        f.write(json.dumps(master_report, indent=2, ensure_ascii=False))

    print(f"Export complete. Summary written to {summary_path}")


if __name__ == "__main__":
    main()
