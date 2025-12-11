from fastapi import APIRouter, Body, Request, HTTPException, status
from fastapi.encoders import jsonable_encoder
from typing import List
from app.schemas.user import UserCreate, UserUpdate, UserResponse
from app.core.database import db
from bson import ObjectId
from datetime import datetime, timezone

router = APIRouter()

@router.post("/", response_description="Add new user", response_model=UserResponse)
async def create_user(user: UserCreate = Body(...)):
    user_dict = jsonable_encoder(user)
    user_dict["created_at"] = datetime.now(timezone.utc)
    user_dict["updated_at"] = datetime.now(timezone.utc)
    
    # Check if username or email already exists
    if await db.get_db()["users"].find_one({"username": user.username}):
        raise HTTPException(status_code=400, detail="Username already exists")
    if await db.get_db()["users"].find_one({"email": user.email}):
        raise HTTPException(status_code=400, detail="Email already exists")

    new_user = await db.get_db()["users"].insert_one(user_dict)
    created_user = await db.get_db()["users"].find_one({"_id": new_user.inserted_id})
    return created_user

@router.get("/", response_description="List all users", response_model=List[UserResponse])
async def list_users(limit: int = 100, skip: int = 0):
    users = await db.get_db()["users"].find().skip(skip).limit(limit).to_list(limit)
    return users

@router.get("/{id}", response_description="Get a single user", response_model=UserResponse)
async def show_user(id: str):
    if (user := await db.get_db()["users"].find_one({"_id": ObjectId(id)})) is not None:
        return user
    raise HTTPException(status_code=404, detail=f"User {id} not found")

@router.put("/{id}", response_description="Update a user", response_model=UserResponse)
async def update_user(id: str, user: UserUpdate = Body(...)):
    user_dict = {k: v for k, v in user.dict().items() if v is not None}

    if len(user_dict) >= 1:
        user_dict["updated_at"] = datetime.now(timezone.utc)
        update_result = await db.get_db()["users"].update_one(
            {"_id": ObjectId(id)}, {"$set": user_dict}
        )

        if update_result.modified_count == 1:
            if (updated_user := await db.get_db()["users"].find_one({"_id": ObjectId(id)})) is not None:
                return updated_user

    if (existing_user := await db.get_db()["users"].find_one({"_id": ObjectId(id)})) is not None:
        return existing_user

    raise HTTPException(status_code=404, detail=f"User {id} not found")

@router.delete("/{id}", response_description="Delete a user")
async def delete_user(id: str):
    delete_result = await db.get_db()["users"].delete_one({"_id": ObjectId(id)})

    if delete_result.deleted_count == 1:
        return {"message": "User deleted successfully"}

    raise HTTPException(status_code=404, detail=f"User {id} not found")
