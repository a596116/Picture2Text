from typing import Optional
from sqlalchemy.orm import Session
from app.crud.crud_base import CRUDBase
from app.models.user import User
from app.schemas.user import UserCreate, UserUpdate
from app.core.security import get_password_hash


class CRUDUser(CRUDBase[User, UserCreate, UserUpdate]):
    """使用者 CRUD 操作"""

    def get_by_email(self, db: Session, *, email: str) -> Optional[User]:
        """根據電子郵件獲取使用者"""
        return db.query(User).filter(User.email == email).first()

    def get_by_username(self, db: Session, *, username: str) -> Optional[User]:
        """根據使用者名稱獲取使用者"""
        return db.query(User).filter(User.username == username).first()

    def create(self, db: Session, *, obj_in: UserCreate) -> User:
        """建立使用者"""
        db_obj = User(
            username=obj_in.username,
            email=obj_in.email,
            hashed_password=get_password_hash(obj_in.password),
            is_active=True,
            is_superuser=False,
        )
        db.add(db_obj)
        db.commit()
        db.refresh(db_obj)
        return db_obj

    def update(self, db: Session, *, db_obj: User, obj_in: UserUpdate) -> User:
        """更新使用者"""
        update_data = obj_in.model_dump(exclude_unset=True)
        if "password" in update_data:
            hashed_password = get_password_hash(update_data["password"])
            del update_data["password"]
            update_data["hashed_password"] = hashed_password
        return super().update(db, db_obj=db_obj, obj_in=update_data)

    def authenticate(self, db: Session, *, username: str, password: str) -> Optional[User]:
        """驗證使用者"""
        from app.core.security import verify_password

        user = self.get_by_username(db, username=username)
        if not user:
            return None
        if not verify_password(password, user.hashed_password):
            return None
        return user

    def is_active(self, user: User) -> bool:
        """檢查使用者是否啟用"""
        return user.is_active

    def is_superuser(self, user: User) -> bool:
        """檢查是否為超級使用者"""
        return user.is_superuser


crud_user = CRUDUser(User)
