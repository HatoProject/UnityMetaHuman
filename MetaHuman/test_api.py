"""
DeepSeek API Test Script
Run this first to verify your API key and model ID work correctly.
"""

import os

# Set your API key
os.environ["ARK_API_KEY"] = "sk-your-api-key-here"

# Install the required package if not already installed
# pip install --upgrade "openai>=1.0"

from openai import OpenAI

# Initialize the client
client = OpenAI(
    base_url="https://ark.cn-beijing.volces.com/api/v3",
    api_key=os.environ.get("ARK_API_KEY"),
)

# Test non-streaming request
print("===== Testing Non-Streaming Request =====")
try:
    completion = client.chat.completions.create(
        model="ep-20260402173502-jqhgv",
        messages=[
            {"role": "system", "content": "你是人工智能助手"},
            {"role": "user", "content": "你好"},
        ],
    )
    print("Response:", completion.choices[0].message.content)
    print("Model:", completion.model)
    print("SUCCESS!")
except Exception as e:
    print(f"ERROR: {e}")

print("\n" + "="*50 + "\n")

# Test streaming request
print("===== Testing Streaming Request =====")
try:
    stream = client.chat.completions.create(
        model="ep-20260402173502-jqhgv",
        messages=[
            {"role": "system", "content": "你是人工智能助手"},
            {"role": "user", "content": "你好"},
        ],
        stream=True,
    )
    print("Response: ", end="")
    for chunk in stream:
        if not chunk.choices:
            continue
        if chunk.choices[0].delta.content:
            print(chunk.choices[0].delta.content, end="")
    print()
    print("SUCCESS!")
except Exception as e:
    print(f"ERROR: {e}")
