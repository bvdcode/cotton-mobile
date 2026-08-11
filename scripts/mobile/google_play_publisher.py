from __future__ import annotations

import json
import logging
from collections.abc import Mapping
from typing import Protocol, cast
from urllib.parse import quote


ANDROID_PUBLISHER_API_BASE_URL = "https://androidpublisher.googleapis.com/androidpublisher/v3/applications"
ANDROID_PUBLISHER_SCOPE = "https://www.googleapis.com/auth/androidpublisher"
ANDROID_PUBLISHER_UPLOAD_BASE_URL = "https://androidpublisher.googleapis.com/upload/androidpublisher/v3/applications"


class GooglePlayPublisherError(Exception):
    pass


class HttpResponse(Protocol):
    status_code: int
    content: bytes
    text: str

    def json(self) -> object:
        ...


class HttpSession(Protocol):
    def request(
        self,
        method: str,
        url: str,
        *,
        json: Mapping[str, object] | None = None,
        data: object | None = None,
        headers: Mapping[str, str] | None = None,
        params: Mapping[str, str] | None = None,
        timeout: int,
    ) -> HttpResponse:
        ...


class CredentialsFactory(Protocol):
    @classmethod
    def from_service_account_info(
        cls,
        info: Mapping[str, object],
        *,
        scopes: list[str],
    ) -> object:
        ...


class ServiceAccountModule(Protocol):
    Credentials: type[CredentialsFactory]


class AuthorizedSessionFactory(Protocol):
    def __call__(self, credentials: object) -> HttpSession:
        ...


class AndroidPublisherClient:
    def __init__(
        self,
        service_account_json: str,
        timeout_seconds: int,
        error_type: type[GooglePlayPublisherError],
        logger: logging.Logger,
    ) -> None:
        self._timeout_seconds = timeout_seconds
        self._error_type = error_type
        self._logger = logger
        service_account, authorized_session_factory = self._load_google_auth_dependencies()
        service_account_info = self._parse_service_account_json(service_account_json)
        credentials = service_account.Credentials.from_service_account_info(
            service_account_info,
            scopes=[ANDROID_PUBLISHER_SCOPE],
        )
        self._session = authorized_session_factory(credentials)

    def create_edit(self, package_name: str) -> str:
        response = self._request("POST", f"{self._application_url(package_name)}/edits", json_body={})
        edit_id = response.get("id")
        if not isinstance(edit_id, str) or not edit_id:
            raise self._error_type("Google Play edit response did not include an edit id.")

        self._logger.info("Created Google Play edit %s.", edit_id)
        return edit_id

    def commit_edit(self, package_name: str, edit_id: str, changes_not_sent_for_review: bool) -> None:
        params: dict[str, str] = {}
        if changes_not_sent_for_review:
            params["changesNotSentForReview"] = "true"

        self._logger.info("Committing Google Play edit %s.", edit_id)
        self._request(
            "POST",
            f"{self._application_url(package_name)}/edits/{self._quote_path(edit_id)}:commit",
            params=params,
        )

    def delete_edit(self, package_name: str, edit_id: str) -> None:
        self._logger.info("Deleting Google Play edit %s.", edit_id)
        response = self._session.request(
            "DELETE",
            f"{self._application_url(package_name)}/edits/{self._quote_path(edit_id)}",
            timeout=self._timeout_seconds,
        )
        if response.status_code >= 400:
            self._logger.warning("Could not delete Google Play edit %s: %s", edit_id, response.text)

    def _request(
        self,
        method: str,
        url: str,
        *,
        json_body: Mapping[str, object] | None = None,
        data: object | None = None,
        headers: Mapping[str, str] | None = None,
        params: Mapping[str, str] | None = None,
    ) -> dict[str, object]:
        response = self._session.request(
            method,
            url,
            json=json_body,
            data=data,
            headers=headers,
            params=params,
            timeout=self._timeout_seconds,
        )
        self._raise_for_error(response)
        if not response.content:
            return {}

        body = response.json()
        if not isinstance(body, dict):
            raise self._error_type("Google Play API response was not a JSON object.")

        return body

    def _raise_for_error(self, response: HttpResponse) -> None:
        if response.status_code < 400:
            return

        try:
            error_body = response.json()
        except ValueError:
            error_body = response.text

        raise self._error_type(
            f"Google Play API request failed with HTTP {response.status_code}: {error_body}"
        )

    def _parse_service_account_json(self, service_account_json: str) -> dict[str, object]:
        try:
            service_account_info = json.loads(service_account_json)
        except json.JSONDecodeError as exception:
            raise self._error_type("Service account JSON is not valid JSON.") from exception

        if not isinstance(service_account_info, dict):
            raise self._error_type("Service account JSON root must be an object.")

        return service_account_info

    def _load_google_auth_dependencies(
        self,
    ) -> tuple[ServiceAccountModule, AuthorizedSessionFactory]:
        try:
            from google.auth.transport.requests import AuthorizedSession
            from google.oauth2 import service_account
        except ModuleNotFoundError as exception:
            raise self._error_type(
                "Google Play upload dependencies are missing. "
                "Install them with: python3 -m pip install google-auth requests"
            ) from exception

        return (
            cast(ServiceAccountModule, service_account),
            cast(AuthorizedSessionFactory, AuthorizedSession),
        )

    @staticmethod
    def _application_url(package_name: str) -> str:
        return f"{ANDROID_PUBLISHER_API_BASE_URL}/{AndroidPublisherClient._quote_path(package_name)}"

    @staticmethod
    def _quote_path(value: str) -> str:
        return quote(value, safe="")
