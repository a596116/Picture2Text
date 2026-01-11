# AI 圖片辨識模型推薦指南

## 📊 當前實現分析

### ✅ 做得好的地方
1. **異步處理** - 使用 `AsyncOpenAI`，支持並發
2. **多模型支持** - 支持 OpenAI 和 Ollama
3. **錯誤處理** - 基本的異常捕獲
4. **結構化輸出** - 返回標準化的 `InvoiceData`

### ⚠️ 需要改進的地方
1. **圖片驗證不足** - 沒有檢查圖片格式、大小
2. **Prompt 可以更優化** - 當前 prompt 較簡單
3. **JSON 解析不夠強健** - 對異常格式處理不足
4. **max_tokens 可能不夠** - 1000 tokens 可能不足以處理複雜發票
5. **沒有使用結構化輸出** - GPT-4o 支持 JSON mode
6. **圖片解析度設置** - 沒有設置 `detail: "high"` 提高識別精度

## 🎯 推薦的模型選擇

### 1. OpenAI 模型（推薦用於生產環境）

#### 🥇 **GPT-4o** (最推薦)
- **優點**：
  - 最新的視覺模型，識別準確度最高
  - 支持高解析度圖片（`detail: "high"`）
  - 支持 JSON mode，輸出更穩定
  - 速度快，成本相對合理
- **適用場景**：生產環境、高準確度要求
- **配置**：
  ```python
  OPENAI_MODEL = "gpt-4o"
  max_tokens = 2000
  temperature = 0.1
  detail = "high"  # 高解析度模式
  ```

#### 🥈 **GPT-4o-mini** (性價比之選)
- **優點**：
  - 成本更低（約 GPT-4o 的 1/10）
  - 速度更快
  - 準確度仍然很高
- **適用場景**：預算有限、大量處理
- **配置**：
  ```python
  OPENAI_MODEL = "gpt-4o-mini"
  max_tokens = 2000
  temperature = 0.1
  ```

#### 🥉 **GPT-4-turbo** (備選)
- **優點**：穩定可靠
- **缺點**：比 GPT-4o 稍慢且成本更高
- **適用場景**：需要穩定性的場景

### 2. Ollama 本地模型（推薦用於開發/測試）

#### 🥇 **llava:34b** (最準確)
- **優點**：
  - 本地運行，數據隱私好
  - 34B 參數，準確度高
  - 免費使用
- **缺點**：
  - 需要較強的硬件（至少 24GB VRAM）
  - 速度較慢
- **適用場景**：數據敏感、本地部署

#### 🥈 **llava:13b** (平衡選擇)
- **優點**：
  - 準確度和速度平衡
  - 硬件要求較低（約 16GB VRAM）
- **適用場景**：中等規模部署

#### 🥉 **llava** (默認版本)
- **優點**：輕量級，速度快
- **缺點**：準確度較低
- **適用場景**：快速測試、簡單發票

### 3. 其他可選模型

#### **Claude 3.5 Sonnet** (Anthropic)
- **優點**：準確度極高，特別擅長文檔理解
- **缺點**：API 可能不如 OpenAI 穩定
- **適用場景**：對準確度要求極高的場景

#### **Google Gemini Pro Vision**
- **優點**：免費額度較高
- **缺點**：準確度略低於 GPT-4o
- **適用場景**：預算極度受限的場景

## 📈 性能對比

| 模型 | 準確度 | 速度 | 成本 | 推薦度 |
|------|--------|------|------|--------|
| GPT-4o | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| GPT-4o-mini | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ |
| GPT-4-turbo | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐ |
| llava:34b | ⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ |
| llava:13b | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ |
| Claude 3.5 | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ |

## 🔧 優化建議

### 1. 使用結構化輸出（JSON Mode）
```python
# GPT-4o 支持
params = {
    "response_format": {"type": "json_object"},
    # ... 其他參數
}
```

### 2. 提高圖片解析度
```python
"image_url": {
    "url": image_url,
    "detail": "high"  # 高解析度模式
}
```

### 3. 優化 Prompt
- 使用更清晰的結構化指令
- 強調 JSON 格式要求
- 提供具體的格式示例

### 4. 增加 Token 限制
- 從 1000 增加到 2000，處理複雜發票

### 5. 降低 Temperature
- 從 0.2 降低到 0.1，提高一致性和準確性

### 6. 添加圖片預處理
- 驗證圖片格式
- 檢查圖片大小
- 支持多種格式（JPEG, PNG, WebP）

## 💡 最佳實踐建議

### 生產環境
1. **首選**：GPT-4o 或 GPT-4o-mini
2. **配置**：
   - `detail: "high"` - 高解析度
   - `max_tokens: 2000` - 足夠的輸出空間
   - `temperature: 0.1` - 高一致性
   - `response_format: {"type": "json_object"}` - 結構化輸出

### 開發/測試環境
1. **首選**：llava:13b 或 llava:34b（本地）
2. **優點**：免費、數據隱私、無 API 限制

### 成本優化
1. **混合策略**：
   - 簡單發票 → GPT-4o-mini
   - 複雜發票 → GPT-4o
2. **緩存機制**：相同圖片只識別一次
3. **批量處理**：合併多張圖片到一個請求（如果 API 支持）

## 🚀 實施建議

1. **短期**（立即實施）：
   - 升級到 GPT-4o 或 GPT-4o-mini
   - 添加 `detail: "high"`
   - 增加 `max_tokens` 到 2000
   - 優化 prompt

2. **中期**（1-2 週）：
   - 實施圖片預處理和驗證
   - 添加結構化輸出支持
   - 實現緩存機制

3. **長期**（1 個月）：
   - 支持多模型切換
   - 實現模型性能監控
   - 添加 A/B 測試框架

## 📝 配置示例

### .env 配置
```env
# 生產環境 - 使用 GPT-4o
AI_SERVICE_TYPE=openai
OPENAI_MODEL=gpt-4o
OPENAI_API_KEY=your-api-key

# 開發環境 - 使用本地模型
# AI_SERVICE_TYPE=ollama
# OLLAMA_MODEL=llava:13b
# OLLAMA_BASE_URL=http://localhost:11434/v1
```

### 代碼配置
```python
# 在 ai_service.py 中
params = {
    "model": "gpt-4o",
    "max_tokens": 2000,
    "temperature": 0.1,
    "response_format": {"type": "json_object"},  # 如果支持
    "image_url": {
        "url": image_url,
        "detail": "high"  # 高解析度
    }
}
```

## 🔍 監控指標

建議監控以下指標：
1. **識別準確率** - 手動驗證樣本
2. **API 響應時間** - 平均處理時間
3. **成本** - 每次識別的成本
4. **錯誤率** - JSON 解析失敗率
5. **重試率** - 需要重試的請求比例
