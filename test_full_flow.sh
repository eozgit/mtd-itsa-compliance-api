#!/bin/bash

# Exit immediately if a command exits with a non-zero status.
set -e

# --- Configuration ---
API_HOST="http://localhost:5129"

# --- 1. Generate random username and email ---
RANDOM_UUID=$(uuidgen | tr -d '-') # Get a UUID and remove hyphens for cleaner names
USERNAME="TestUser_${RANDOM_UUID:0:8}"
EMAIL="${USERNAME}@example.com"
PASSWORD="Password123!"
BUSINESS_NAME="My Automated Business for ${USERNAME}"
START_DATE="2025-04-06T00:00:00"

echo "--- Generated Test Data ---"
echo "Username: $USERNAME"
echo "Email: $EMAIL"
echo "--------------------------"
echo ""

# --- 2. Register User ---
echo "--- Registering User ---"
REGISTER_PAYLOAD=$(jq -n \
  --arg email "$EMAIL" \
  --arg password "$PASSWORD" \
  --arg userName "$USERNAME" \
  '{email: $email, password: $password, userName: $userName}')

REGISTER_RESPONSE=$(curl -s -X POST "${API_HOST}/api/auth/register" \
  -H 'Content-Type: application/json' \
  -d "$REGISTER_PAYLOAD")

echo "Register Response:"
echo "$REGISTER_RESPONSE" | jq . || echo "ERROR: Could not parse JSON response for register."
echo ""

# --- 3. Login User and save token ---
echo "--- Logging In User ---"
LOGIN_PAYLOAD=$(jq -n \
  --arg email "$EMAIL" \
  --arg password "$PASSWORD" \
  '{email: $email, password: $password}')

LOGIN_RESPONSE=$(curl -s -X POST "${API_HOST}/api/auth/login" \
  -H 'Content-Type: application/json' \
  -d "$LOGIN_PAYLOAD")

echo "Login Response:"
echo "$LOGIN_RESPONSE" | jq . || echo "ERROR: Could not parse JSON response for login."
echo ""

AUTH_TOKEN=$(echo "$LOGIN_RESPONSE" | jq -r '.token')
USER_ID=$(echo "$LOGIN_RESPONSE" | jq -r '.userId')

if [ -z "$AUTH_TOKEN" ] || [ "$AUTH_TOKEN" == "null" ]; then
  echo "ERROR: Failed to retrieve AUTH_TOKEN. Exiting."
  exit 1
fi

echo "Extracted Auth Token: $AUTH_TOKEN"
echo ""

# --- 4. Register a Business ---
echo "--- Registering Business ---"
BUSINESS_PAYLOAD=$(jq -n \
  --arg name "$BUSINESS_NAME" \
  --arg startDate "$START_DATE" \
  '{name: $name, startDate: $startDate}')

BUSINESS_RESPONSE=$(curl -s -X POST "${API_HOST}/api/business" \
  -H 'Content-Type: application/json' \
  -H "Authorization: Bearer $AUTH_TOKEN" \
  -d "$BUSINESS_PAYLOAD")

echo "Business Registration Response:"
echo "$BUSINESS_RESPONSE" | jq . || echo "ERROR: Could not parse JSON response for business registration."
echo ""

# --- 5. Get Quarters and save the last quarter's ID ---
echo "--- Getting Quarters ---"
QUARTERS_RESPONSE=$(curl -s -X GET "${API_HOST}/api/quarters" \
  -H "Authorization: Bearer $AUTH_TOKEN")

echo "Quarters Response:"
echo "$QUARTERS_RESPONSE" | jq . || echo "ERROR: Could not parse JSON response for get quarters."
echo ""

# Extract the ID of the last quarter from the "quarters" array
LAST_QUARTER_ID=$(echo "$QUARTERS_RESPONSE" | jq -r '.quarters[-1].id')

if [ -z "$LAST_QUARTER_ID" ] || [ "$LAST_QUARTER_ID" == "null" ]; then
  echo "ERROR: Failed to retrieve LAST_QUARTER_ID. Exiting."
  exit 1
fi

echo "ID of the last quarter: $LAST_QUARTER_ID"
echo ""

# --- 6. Update a Specific Quarter ---
echo "--- Updating Quarter $LAST_QUARTER_ID ---"
UPDATE_QUARTER_PAYLOAD=$(jq -n \
  --arg taxableIncome 2500.75 \
  --arg allowableExpenses 1200.50 \
  '{taxableIncome: $taxableIncome, allowableExpenses: $allowableExpenses}')

UPDATE_QUARTER_RESPONSE=$(curl -s -X PUT "${API_HOST}/api/quarter/${LAST_QUARTER_ID}" \
  -H 'Content-Type: application/json' \
  -H "Authorization: Bearer $AUTH_TOKEN" \
  -d "$UPDATE_QUARTER_PAYLOAD")

echo "Update Quarter Response:"
echo "$UPDATE_QUARTER_RESPONSE" | jq . || echo "ERROR: Could not parse JSON response for update quarter."
echo ""

# --- 7. Submit a Specific Quarter ---
echo "--- Submitting Quarter $LAST_QUARTER_ID ---"
SUBMIT_QUARTER_RESPONSE=$(curl -s -X POST "${API_HOST}/api/quarter/${LAST_QUARTER_ID}/submit" \
  -H "Authorization: Bearer $AUTH_TOKEN")

echo "Submit Quarter Response:"
echo "$SUBMIT_QUARTER_RESPONSE" | jq . || echo "ERROR: Could not parse JSON response for submit quarter."
echo ""

echo "--- Full Automation Script Finished Successfully ---"
