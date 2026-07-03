package com.ratepulse.android

data class CurrencyOption(
    val code: String,
    val chineseName: String,
    val englishName: String
) {
    val label: String = "$chineseName ($code)"
}

object RatePulseCurrencies {
    val options: List<CurrencyOption> = listOf(
        CurrencyOption("CNY", "人民币", "Chinese Yuan"),
        CurrencyOption("EUR", "欧元", "Euro"),
        CurrencyOption("JPY", "日元", "Japanese Yen"),
        CurrencyOption("GBP", "英镑", "British Pound"),
        CurrencyOption("TRY", "土耳其里拉", "Turkish Lira"),
        CurrencyOption("HKD", "港元", "Hong Kong Dollar"),
        CurrencyOption("AUD", "澳元", "Australian Dollar"),
        CurrencyOption("CAD", "加元", "Canadian Dollar"),
        CurrencyOption("CHF", "瑞士法郎", "Swiss Franc"),
        CurrencyOption("SGD", "新加坡元", "Singapore Dollar"),
        CurrencyOption("KRW", "韩元", "South Korean Won"),
        CurrencyOption("THB", "泰铢", "Thai Baht"),
        CurrencyOption("INR", "印度卢比", "Indian Rupee"),
        CurrencyOption("MXN", "墨西哥比索", "Mexican Peso"),
        CurrencyOption("BRL", "巴西雷亚尔", "Brazilian Real"),
        CurrencyOption("NZD", "新西兰元", "New Zealand Dollar"),
        CurrencyOption("SEK", "瑞典克朗", "Swedish Krona"),
        CurrencyOption("NOK", "挪威克朗", "Norwegian Krone"),
        CurrencyOption("DKK", "丹麦克朗", "Danish Krone"),
        CurrencyOption("ZAR", "南非兰特", "South African Rand"),
        CurrencyOption("USD", "美元", "US Dollar")
    )

    fun normalize(code: String?): String {
        val normalized = code.orEmpty().trim().uppercase()
        return if (normalized.length == 3) normalized else DEFAULT_TARGET
    }

    fun labelFor(code: String): String {
        val normalized = normalize(code)
        return options.firstOrNull { it.code == normalized }?.label ?: normalized
    }

    fun indexOf(code: String): Int {
        val normalized = normalize(code)
        return options.indexOfFirst { it.code == normalized }.takeIf { it >= 0 } ?: 0
    }

    const val DEFAULT_TARGET = "CNY"
}
