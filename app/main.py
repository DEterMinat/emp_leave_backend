from fastapi import FastAPI
from fastapi.responses import HTMLResponse
from contextlib import asynccontextmanager
from app.core.config import settings
from app.core.database import db
from app.routers import example, users

@asynccontextmanager
async def lifespan(app: FastAPI):
    # Startup
    db.connect()
    yield
    # Shutdown
    db.close()

app = FastAPI(
    title=settings.PROJECT_NAME,
    lifespan=lifespan,
    docs_url=None, # Disable default Swagger UI to use Scalar or keep both
    redoc_url=None
)

# Include Routers
app.include_router(example.router, tags=["Example"])
app.include_router(users.router, prefix="/users", tags=["Users"])

# Scalar API Reference
@app.get("/docs", include_in_schema=False)
async def scalar_html():
    return HTMLResponse(
        """
        <!doctype html>
        <html>
          <head>
            <title>Scalar API Reference</title>
            <meta charset="utf-8" />
            <meta
              name="viewport"
              content="width=device-width, initial-scale=1" />
            <style>
              body {
                margin: 0;
              }
            </style>
          </head>
          <body>
            <script
              id="api-reference"
              data-url="/openapi.json"></script>
            <script src="https://cdn.jsdelivr.net/npm/@scalar/api-reference"></script>
          </body>
        </html>
        """
    )

@app.get("/")
async def root():
    return {"message": "Go to /docs for API documentation"}
