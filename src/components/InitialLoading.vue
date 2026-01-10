<template>
  <!-- 初始載入狀態：首次上傳檔案時顯示的載入畫面 -->
  <div
    class="min-h-screen bg-[#F5F5F7] flex flex-col items-center justify-center p-6 text-center relative overflow-hidden"
  >
    <!-- 背景氛圍效果 -->
    <div
      class="fixed top-0 left-0 w-full h-full overflow-hidden -z-10 pointer-events-none"
    >
      <div
        class="absolute -top-[20%] -left-[10%] w-[50%] h-[50%] bg-blue-200/30 rounded-full blur-[100px] animate-breathe"
      />
      <div
        class="absolute top-[40%] -right-[10%] w-[40%] h-[60%] bg-purple-200/30 rounded-full blur-[100px] animate-breathe"
        style="animation-delay: 1s"
      />
    </div>

    <div class="max-w-md w-full space-y-8 relative z-10 animate-in fade-in">
      <!-- 掃描器圖標區域 -->
      <div class="flex justify-center py-8">
        <div class="relative w-48 h-48 flex items-center justify-center">
          <!-- 外層脈衝環動畫（多層以增強效果） -->
          <div class="absolute inset-0 flex items-center justify-center">
            <div class="w-48 h-48 bg-blue-100/50 rounded-full pulse-ring"></div>
          </div>
          <div class="absolute inset-0 flex items-center justify-center">
            <div
              class="w-48 h-48 bg-blue-100/30 rounded-full pulse-ring"
              style="animation-delay: 0.5s"
            ></div>
          </div>
          <!-- 中間圓形容器 -->
          <div
            class="relative w-40 h-40 bg-slate-100 rounded-full flex items-center justify-center shadow-lg z-10"
          >
            <!-- 內層白色圓形 -->
            <div
              class="w-32 h-32 bg-white rounded-full flex items-center justify-center shadow-inner"
            >
              <!-- 掃描器圖標（四個 L 形角落） -->
              <div class="relative w-20 h-20 flex items-center justify-center">
                <!-- 左上角 -->
                <div
                  class="absolute top-0 left-0 w-8 h-8 border-t-4 border-l-4 border-blue-600 rounded-tl-lg"
                ></div>
                <!-- 右上角 -->
                <div
                  class="absolute top-0 right-0 w-8 h-8 border-t-4 border-r-4 border-blue-600 rounded-tr-lg"
                ></div>
                <!-- 左下角 -->
                <div
                  class="absolute bottom-0 left-0 w-8 h-8 border-b-4 border-l-4 border-blue-600 rounded-bl-lg"
                ></div>
                <!-- 右下角 -->
                <div
                  class="absolute bottom-0 right-0 w-8 h-8 border-b-4 border-r-4 border-blue-600 rounded-br-lg"
                ></div>
                <!-- 中心點 -->
                <div class="w-2 h-2 bg-blue-600 rounded-full"></div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- 文字內容 -->
      <div class="space-y-2 mt-8">
        <h2 class="text-2xl font-bold text-slate-900">分析中...</h2>
        <p class="text-slate-500">正在準備您的發票</p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
// 初始載入組件 - 用於首次上傳檔案時的載入動畫
</script>
