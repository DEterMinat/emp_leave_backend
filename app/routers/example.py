from fastapi import APIRouter, Depends
from app.core.database import get_database

router = APIRouter()

@router.get("/")
async def read_root():
    return {"message": "Welcome to Employee Leave System API"}

@router.get("/health")
async def health_check(db = Depends(get_database)):
    try:
        # Ping the database
        await db.command("ping")
        return {"status": "ok", "database": "connected"}
    except Exception as e:
        return {"status": "error", "database": str(e)}
